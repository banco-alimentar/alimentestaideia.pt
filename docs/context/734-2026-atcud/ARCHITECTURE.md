# Arquitetura — Recibos fiscais portugueses com ATCUD, Código QR e SAF-T (PT)

## Princípios

Um recibo fiscal é um registo imutável emitido uma vez para uma doação paga, e não um PDF reconstruído a partir de dados atuais. A operação crítica deve, numa única transação SQL, resolver o âmbito fiscal, reservar a sequência, criar a fotografia fiscal, calcular assinatura, ATCUD e QR, e persistir o documento e auditoria.

Os dados operacionais fiscais pertencem ao `ApplicationDbContext` de cada tenant. O `InfrastructureDbContext` mantém apenas identificação de tenant, domínios, estratégias e referências de infraestrutura. Não guardar no contexto de infraestrutura contadores, ATCUD emitidos, documentos, SAF-T, auditoria ou segredos.

```text
Sas.Model / Sas.Core / Sas.ConfigurationProvider
  └─ tenant, estratégia e referências Key Vault
Model
  └─ entidades persistidas no tenant
Repository
  └─ emissão, assinaturas, QR, SAF-T, auditoria e concorrência
Web
  └─ autorização, administração, renderização e entrega
```

Não colocar regras fiscais em Razor Pages. A camada Web recebe ou pede um documento fiscal finalizado; não recalcula valores.

## Âmbito fiscal e multi-tenancy

Criar `IFiscalScopeResolver`, que recebe o tenant atual, a estratégia e a doação carregada e devolve tenant, estratégia, emitente, `FoodBank` opcional, perfil fiscal ativo e série aplicável.

| Estratégia | Emitente e chave de segregação |
|---|---|
| `SingleInvoiceTable` | Emitente do tenant; chave baseada no tenant persistido. |
| `MultipleTablesPerFoodBank` | Emitente associado ao `FoodBank` da doação; chave tenant + `FoodBankId`. |

O resolvedor bloqueia a emissão antes de reservar numeração se o âmbito, perfil ou série for inválido. Alterar `InvoicingStrategy` depois de existirem documentos fiscais requer um processo administrativo explícito; nunca pode reinterpretar documentos já emitidos.

## Modo local de simulação fiscal

O modo de simulação é um percurso de desenvolvimento distinto, não uma variação de
um documento fiscal. A decisão é feita uma única vez no servidor antes de resolver
perfil, série ou chave fiscal. A aplicação mantém `IFiscalDocumentIssuanceService`
como fachada do fluxo de recibo e delega para uma das implementações abaixo:

```text
IFiscalDocumentIssuanceService
  ├─ RealFiscalDocumentIssuanceWorkflow
  │    └─ perfil + série AT + assinatura fiscal + QR AT + PDF fiscal
  └─ SimulatedFiscalDocumentIssuanceWorkflow
       └─ âmbito + sequência sintética + checksum local + QR privado + PDF simulado
```

`IFiscalIssuanceModeResolver` é a única porta que escolhe o percurso. Recebe
`IHostEnvironment` e `IOptions<FiscalSimulationOptions>`; não recebe dados de um
pedido HTTP, cookie, cabeçalho, utilizador, tenant ou Banco Alimentar. O seu contrato
devolve `Real` ou `Simulation`, ou falha de forma explícita. Nenhum erro no percurso
simulado pode fazer *fallback* para emissão fiscal real, nem o percurso real pode
consultar documentos, contadores ou artefactos simulados.

### Ativação fail-closed

Definir uma única configuração, `Fiscal:Simulation:Enabled`, com valor predefinido
`false`. O registo das dependências e a validação de opções devem obedecer às regras
seguintes:

1. A simulação só é elegível quando `IHostEnvironment.EnvironmentName` é exatamente
   `Development` e a opção tem valor booleano explícito `true`.
2. Se a opção estiver ativa em `Testing`, `Staging`, `PreProduction`, `Production` ou
   num nome de ambiente desconhecido, a aplicação falha no arranque com uma mensagem
   que identifique a configuração indevida. Não se regista o fluxo simulado nesses
   ambientes.
3. Configuração ausente, inválida, ou qualquer falha ao determinar o ambiente não
   ativa simulação. A emissão simulada é bloqueada e auditada; nunca se emite um
   artefacto ambíguo.
4. Não criar uma opção por tenant, por Banco Alimentar ou por utilizador, nem uma
   opção de administração que alargue este limite. A configuração deve permanecer
   local ao processo de desenvolvimento e não pode constar de definições de
   preprodução ou produção.

O início de cada emissão simulada regista um evento de auditoria com ambiente,
identificador do tenant, âmbito fiscal, utilizador, correlação e motivo
`DevelopmentSimulation`; não regista segredos, conteúdo de doação desnecessário ou
configuração de chaves.

### Persistência e separação semântica

Não representar uma simulação como `FiscalDocument` com uma `bool` editável. Criar
tabelas próprias no `ApplicationDbContext` de cada tenant:

- `SimulatedFiscalDocument`: doação, pagamento, âmbito, identificador sintético,
  sequência, fotografia mínima de demonstração, checksum local, QR privado,
  referência do artefacto, ambiente de origem e dados de criação imutáveis;
- `SimulatedFiscalSequence`: contador por chave de âmbito simulado;
- `SimulatedFiscalAuditEvent` ou uma categoria imutável equivalente no diário de
  auditoria, sempre com `IssuanceMode = Simulation`.

`SimulatedFiscalDocument` não deve ter colunas `Atcud`, `AtValidationCode`,
`FiscalHash`, `HashControl`, `SoftwareCertificateNumber`, `SigningKeyVersion` ou
`AtCommunicationReference`. O seu identificador usa um formato reservado, como
`SIM-<ambito>-<sequencia>`, e não é apresentado, pesquisado ou exportado como ATCUD.
Restrições únicas em `DonationId` e em `ScopeKey + Sequence`, acompanhadas por
concorrência SQL/`rowversion`, preservam a idempotência e a unicidade sem tocar nos
contadores de séries reais.

Os documentos simulados nunca passam por uma operação de promoção. Não expor uma
atualização de entidade que converta entre tabelas, nem uma migração de dados que
copie simulações para a tabela fiscal. O acesso de leitura pode usar uma projeção
unificada para a página Admin, mas deve transportar um discriminador obrigatório e
recusar ações fiscais (anulação, retificação, download fiscal e exportação oficial)
quando o valor for `Simulation`.

### Contratos de artefacto e entrega

Manter os contratos fiscais existentes exclusivamente para o fluxo real:
`IFiscalSignatureService`, `IFiscalSigningKeyProvider`,
`IFiscalQrPayloadBuilder`, `IFiscalPdfRenderer` e `ISaftExportService`. Introduzir
contratos distintos para a simulação:

- `ISimulatedFiscalIdentifierAllocator`, para a reserva atómica de sequência;
- `ISimulationChecksumService`, para uma verificação local que não se denomina hash
  ou assinatura fiscal e não usa chave/certificado fiscal;
- `ISimulatedReceiptQrPayloadBuilder`, que codifica uma versão privada, por exemplo
  `AEI-SIM/v1`, e nunca os campos ou a gramática QR da Portaria n.º 195/2020;
- `ISimulatedReceiptPdfRenderer`, que recebe apenas um modelo de simulação e imprime
  `SIMULAÇÃO — SEM VALIDADE FISCAL` em todas as páginas;
- `ISimulatedReceiptArtifactDeliveryService`, que entrega apenas artefactos com
  nome `SIMULACAO-...`, content-disposition e metadados de simulação explícitos.

Estes contratos podem partilhar adaptadores técnicos de armazenamento e email com o
percurso real, mas recebem tipos de pedido/resposta diferentes. Os blobs simulados
ficam num prefixo separado, por exemplo `simulation/development/<tenant>/<scope>/`,
com metadados `issuance-mode=simulation` e `origin-environment=Development`. O
serviço de entrega real deve recusar tal prefixo. A entrega por email, o download e
a reimpressão preservam a mesma marca, e uma reimpressão devolve o artefacto já
persistido em vez de criar outra sequência.

### Âmbito, consulta e exclusões

`IFiscalScopeResolver` continua a resolver tenant e `FoodBank` a partir da doação e
da estratégia configurada, mas a implementação simulada não requer perfil de
emitente ou série AT. Em `SingleInvoiceTable`, a sequência simulada é isolada por
tenant. Em `MultipleTablesPerFoodBank`, é isolada por tenant e `FoodBankId` da
doação. As mesmas verificações de autorização, pertença de doação, idempotência e
tokens de download aplicam-se antes da consulta ou entrega de uma simulação.

`ISaftExportService` consulta exclusivamente `FiscalDocument`; não aceita a tabela
simulada, mesmo se uma projeção administrativa a conseguir mostrar. Não existe um
ficheiro de diagnóstico chamado SAF-T (PT). Se for indispensável exportar dados de
diagnóstico locais, deve usar formato, rota, extensão e marca de simulação distintos
e permanecer bloqueado fora de `Development`. Anulação e retificação fiscais rejeitam
referências a `SimulatedFiscalDocument`.

## Preservação da emissão fiscal real

Com `Fiscal:Simulation:Enabled` ausente ou `false`, e em todos os ambientes não
`Development`, a emissão segue sem alterações o percurso fiscal real: exige perfil,
série comunicada, validações de ambiente, certificado, assinatura fiscal, ATCUD, QR
legal e retenção do PDF fiscal. O modo simulado não relaxa estas pré-condições, não
altera o esquema SAF-T e não substitui o ambiente de testes da AT. Os dois percursos
partilham apenas a elegibilidade operacional da doação, autorização e infraestrutura
genérica de artefactos; os campos fiscais, o QR, a assinatura, a numeração e a
exportação permanecem isolados.

## Modelo de dados

Criar no `ApplicationDbContext`:

- `FiscalIssuerProfile`: emitente legal, âmbito tenant/`FoodBank`, identidade e morada fiscal, tipo SAF-T, IVA/isenção, certificado aplicado, referência e versão segura da chave, estado e vigência.
- `FiscalDocumentSeries`: emitente, tipo SAF-T, meio de processamento, série, primeiro/último número, código de validação AT, estado, datas e evidência de comunicação.
- Documento fiscal ou extensão fiscal de `Invoice`: doação, pagamento, emitente/série, número, ATCUD, `Hash`, `HashControl`, versão de chave, payload QR, totais e linhas, fotografia do emitente/doador, certificado, estado, anulação/retificação, PDF e marca de histórico.
- `FiscalAuditEvent`, append-only: ator, data/hora UTC, tenant/emitente/série/documento, ação, resultado e correlação, sem segredos ou dados pessoais excessivos.

Impor índices únicos para emitente+tipo+série, emitente+código de validação AT quando presente, série+sequencial, documento+ATCUD e a chave idempotente da emissão. Usar limites, campos obrigatórios, relações e `rowversion`; não usar `nvarchar(max)` para campos fiscais críticos.

Os campos históricos devem ser tratados assim:

- `TaxRegistrationNumber`: evolui para o NIF do `FiscalIssuerProfile`.
- `SoftwareCertificateNumber`: configuração versionada e fotografia no documento.
- `HashCypher`: não migrar nem usar para segredos; substituir por referência segura e versão de chave.
- `ATCUD`: não é configuração de tenant/`FoodBank`; é derivado e persistido por documento.

Não copiar a migração histórica nem executar a remoção de `Donations.Referral`.

## Emissão, concorrência e recuperação

Substituir `InvoiceRepository.GetOrCreateInvoiceByDonation` para documentos fiscais por `IFiscalDocumentIssuanceService`, usado por downloads, emails e fluxos de pagamento. O algoritmo é:

1. Carregar doação e pagamento confirmado.
2. Resolver e validar o âmbito fiscal, perfil, série, certificado, chave e ambiente.
3. Procurar documento idempotente já emitido.
4. Reservar o próximo número atomicamente na série com SQL/`rowversion`, nunca `Max`, contagem, cache ou `lock (this)`.
5. Criar fotografia canónica, ATCUD, assinatura e payload QR.
6. Persistir documento, linhas, auditoria e contador da série na mesma transação.
7. Gerar e armazenar uma única representação PDF determinística; falhas de ficheiro são recuperáveis sem alterar o documento.

Downloads e reimpressões servem o PDF final. O PDF e SAF-T não podem depender de alterações posteriores a utilizadores, doações, perfis ou configuração visual.

## Assinatura, QR e SAF-T

Criar contratos injetáveis: `IFiscalSignatureService`, `IFiscalSigningKeyProvider`, `IFiscalQrPayloadBuilder`, `IFiscalPdfRenderer` e `ISaftExportService`. A chave privada é não exportável e obtida/operada por Azure Key Vault ou HSM através da configuração de Key Vault existente. Guardar apenas referência e versão. Nunca guardar chaves em SQL, `Tenant`, `FoodBank`, `appsettings`, logs, telemetria, HTML ou blobs.

O serviço QR recebe apenas a fotografia fiscal final, constrói e persiste o payload canónico AT e usa `QRCoder` somente para rasterização. Não usar NIF, ATCUD, certificado ou campos fixos/de exemplo. Ambos os templates de recibo usam um modelo fiscal comum e imprimem identificação do emitente, tipo/série/número, data, totais, IVA/isenção, ATCUD, QR, excerto da assinatura e certificado.

O exportador SAF-T consulta apenas fotografias de documentos finalizados, inclui estados de anulação/retificação e gera XML determinístico validável contra o esquema AT aplicável. Deve exigir tenant, emitente e autorização explícitos.

## Anulação, autorização e compatibilidade

O caminho atual de `CancelInvoiceModel` não pode alterar diretamente `IsCanceled` em documentos fiscais. Criar comando autorizado que preserva original, número, ATCUD, assinatura, QR e PDF; regista motivo e auditoria; e aplica apenas transições de estado autorizadas. Retificações são novos documentos ligados ao original, com nova série/número/ATCUD/assinatura.

Toda a consulta por número, série, ATCUD ou ID recebe o `FiscalScope`. Tokens de download incluem/validam tenant. As operações administrativas usam política explícita e validam tenant e, quando aplicável, `FoodBank`.

Recibos históricos permanecem inalterados e distinguem-se dos novos documentos certificados. A migração é faseada: tabelas novas, classificação de históricos, perfis/séries configurados sem valores inventados, e ativação apenas quando todos os dados AT e de chave estiverem presentes.

## Testes e regras de implementação

Usar `DateTimeOffset`/UTC e converter explicitamente para Portugal; usar `decimal` para todos os valores fiscais novos. Testar perfis e séries inválidos, ambas as estratégias de faturação, emissão concorrente e idempotente, rotação de chave, QR e imagem decodificável, reimpressão imutável, anulação/retificação, SAF-T/XSD, segregação e históricos. Testes de concorrência e índices devem usar SQL Server ou equivalente relacional; EF InMemory não é suficiente.

Para a simulação, acrescentar fronteiras de teste explícitas:

- testes unitários de `IFiscalIssuanceModeResolver` para opção ausente, `false`,
  `true` em `Development` e `true` em cada ambiente proibido, incluindo ambiente
  desconhecido;
- teste de composição/arranque que prova que uma configuração de simulação fora de
  `Development` falha antes de poder registar ou resolver o emissor simulado;
- testes de integração browser em `Development` para doação elegível, repetição,
  isolamento tenant/`FoodBank`, autorização, reserva concorrente e consulta Admin;
- testes de persistência relacionais que verificam índices únicos, separação de
  tabelas, imutabilidade e ausência de campos/valores fiscais no documento simulado;
- testes de artefacto que decodificam o QR privado, confirmam que não é um payload AT,
  verificam a marca exata em todas as páginas do PDF e validam nome/metadados de
  entrega;
- testes de regressão que garantem que a exportação SAF-T, anulação, retificação e
  entrega fiscal real ignoram ou recusam documentos simulados; e
- testes do percurso real, com a simulação desligada, que asseguram que a respetiva
  validação de perfil, série, assinatura e ATCUD não foi enfraquecida.

Não usar o modo de simulação para provar conformidade AT, compatibilidade de
assinatura, QR legal ou SAF-T. Essas verificações continuam a exigir o ambiente de
testes AT, artefactos assinados de acordo com a especificação aplicável e validação
contra o XSD oficial.
