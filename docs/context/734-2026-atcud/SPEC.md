# Especificação de produto — Recibos fiscais portugueses com ATCUD, Código QR e SAF-T (PT)

## Objetivo

Preparar o módulo de emissão de recibos de donativos para cumprir os requisitos funcionais necessários à certificação do programa de faturação pela Autoridade Tributária e Aduaneira (AT), incluindo ATCUD, Código QR, assinatura de documentos, exportação SAF-T (PT), rastreabilidade e operação multi-tenant.

A certificação é atribuída pela AT ao programa e à versão submetida, após a declaração Modelo 24 e eventual verificação de conformidade. Esta funcionalidade deve produzir os documentos, registos e evidências necessários para a candidatura, mas não substitui a validação formal da AT nem a validação fiscal do Contabilista Certificado (CC) de cada entidade emitente.

## Resultado esperado

Quando uma doação paga requer recibo, o sistema emite uma única versão fiscal do recibo, associada ao emitente fiscal correto, à série autorizada pela AT e ao pagamento respetivo. O recibo apresenta todos os elementos legais aplicáveis, incluindo ATCUD, Código QR, assinatura e referência ao certificado do programa. O sistema conserva um registo imutável e auditável do documento e permite a sua exportação em SAF-T (PT).

## Âmbito

Inclui:

- Recibos de donativos emitidos pela aplicação.
- Documentos de anulação e retificação relacionados com esses recibos, quando aplicáveis.
- Configuração e ciclo de vida de emitentes fiscais, séries documentais e códigos de validação AT.
- Geração, impressão e conservação do ATCUD e do Código QR.
- Assinatura eletrónica fiscal dos documentos.
- Exportação SAF-T (PT).
- Rastreabilidade, controlo de acesso e auditoria.
- Compatibilidade com `SingleInvoiceTable` e `MultipleTablesPerFoodBank`.
- Migração segura de recibos históricos e respetiva consulta.

Não inclui a submissão do Modelo 24 em nome do produtor do software, a atribuição do número de certificação pela AT, a decisão fiscal sobre a natureza de cada recibo, ou outros tipos documentais sem relação com o ciclo atual de recibos.

## Modo local de simulação fiscal

O produto deve disponibilizar um modo de simulação fiscal local para que, em
`Development`, um utilizador possa percorrer no browser a emissão, a entrega e a
consulta de um recibo sem comunicar séries à AT, sem certificado de software e sem
Azure Key Vault. Este modo é uma ferramenta de desenvolvimento: não emite, não tenta
imitar nem declara emitir documentos fiscalmente válidos.

### Ativação e limites de segurança

- O modo é desativado por omissão e requer uma configuração explícita de desenvolvimento.
- Só pode ser ativado se o ambiente efetivo for exatamente `Development`. A configuração,
  uma variável de ambiente, um tenant ou um pedido HTTP não o podem ativar em `Testing`,
  `Staging`, `PreProduction`, `Production` ou num ambiente cujo nome não seja reconhecido.
- Em qualquer ambiente diferente de `Development`, a tentativa de o ativar deve falhar no
  arranque com erro acionável e não pode registar nem emitir um documento simulado.
- A decisão de simulação é tomada no servidor, nunca por um parâmetro, cookie, cabeçalho
  ou campo enviado pelo browser. Deve ser auditada com ambiente, tenant, utilizador e
  motivo, sem segredos.
- A simulação aplica as mesmas regras de autorização, isolamento por tenant, âmbito de
  Banco Alimentar, elegibilidade da doação, idempotência e concorrência da emissão normal.
  Não permite contornar permissões nem consultar artefactos de outro âmbito fiscal.

### Percurso do utilizador em desenvolvimento

1. Um operador autorizado ativa a configuração local e entra num tenant de desenvolvimento
   com uma doação de teste elegível.
2. Ao pedir o recibo pelo fluxo normal, a aplicação identifica no servidor que a simulação
   está ativa e cria um documento de simulação no âmbito fiscal correto, sem exigir perfil
   de emitente, série comunicada, código `AA`, Key Vault ou chamada à AT.
3. A aplicação atribui uma identificação sintética, por exemplo `SIM-<âmbito>-<sequência>`.
   Esta identificação não é um ATCUD, não usa um código de validação da AT e não deve ser
   apresentada com o rótulo `ATCUD`.
4. A aplicação produz e entrega o PDF pelo mesmo percurso de download/email usado para o
   recibo fiscal, com QR de simulação e checksum local apenas para verificar repetição.
   Um novo pedido devolve o mesmo artefacto simulado e não consome outra sequência.
5. O operador pode consultar os documentos simulados no Admin e confirmar estado,
   identificador sintético, sequência, âmbito, artefacto e auditoria. Os documentos de
   simulação são sempre visualmente e semanticamente distintos dos documentos fiscais.

### Conteúdo e identificação dos artefactos

- A página HTML, PDF, nome do ficheiro de download e metadados de entrega devem conter,
  de forma visível e não removível, a expressão exata `SIMULAÇÃO — SEM VALIDADE FISCAL`.
  No PDF, a marca deve surgir em todas as páginas e não apenas no texto opcional do corpo.
- O QR de simulação deve codificar um formato privado inequivocamente não AT, com versão e
  identificador sintético. Não pode conter a estrutura do QR da Portaria n.º 195/2020, nem
  ser apresentado como QR fiscal.
- A simulação não cria ATCUD, código de validação de série AT, assinatura fiscal, hash fiscal,
  certificado de software, referência de comunicação AT ou evidência de comunicação de série.
  Um checksum local, se existir, deve usar nome, campos e apresentação distintos da assinatura
  fiscal.
- A resposta de download deve usar um nome de ficheiro iniciado por `SIMULACAO-` e metadados
  explícitos de simulação. O conteúdo não pode ser reutilizado ou promovido a documento fiscal
  depois de sair de `Development`.

### Dados, exportação e transição

- Cada documento e artefacto simulado deve ser marcado persistentemente como simulação e
  conservar o ambiente de origem. Não pode ser convertido para documento fiscal válido,
  alterando uma flag, uma série ou o ambiente.
- A exportação SAF-T oficial não pode incluir documentos de simulação. Se for criada uma
  exportação para diagnóstico local, deve ter formato/ficheiro distinto e a mesma marca de
  simulação; não pode ser designada SAF-T (PT) oficial.
- A aplicação não pode migrar, copiar, reenviar ou disponibilizar documentos simulados em
  ambientes não `Development`. Dados de desenvolvimento devem permanecer isolados dos dados
  de preprodução e produção.
- A anulação e a retificação fiscais não se aplicam a documentos simulados. Um teste pode
  exercer apenas estados de demonstração claramente identificados como simulação, sem gerar
  documentos de anulação ou retificação fiscal.

### Exclusões explícitas

O modo de simulação não substitui o ambiente de testes da AT, a comunicação de séries, a
certificação do software, a assinatura fiscal, a emissão de documentos reais, a comunicação de
faturas, a exportação SAF-T oficial ou a validação do CC. Não deve ser oferecido como opção de
administração a clientes, utilizadores de produção ou operadores de preprodução.

## Dependência fiscal de entrada em produção

Antes de ativar a emissão fiscal em produção, o CC deve validar, por emitente, a tipologia SAF-T do recibo de donativo, NIF e identificação fiscal, IVA ou isenção, séries a comunicar à AT, anulações e retificações, documentos de anos anteriores e obrigações de comunicação. O produto deve suportar estes valores por emitente e bloquear a emissão fiscal quando a configuração estiver ausente, inválida ou incoerente.

## Requisitos funcionais

### Perfis de emitente fiscal

O sistema deve configurar um perfil fiscal por entidade emitente com identificação legal e NIF, morada, configuração fiscal dos recibos, séries autorizadas, referência à versão e certificado do programa, datas e estado de ativação, e registo do responsável por cada alteração. A configuração fiscal deve ser separada da identidade visual do recibo.

### Séries e ATCUD

O sistema deve gerir séries por emitente, tipo documental e meio de processamento. Para cada série deve guardar identificador, tipo, emitente, data de início, primeiro e último números, código de validação AT, estado e evidência de comunicação. O ATCUD de cada documento é `ATCUD:<código-de-validação-da-série>-<número-sequencial>`; o código de validação não pode ser reutilizado e o ATCUD atribuído mantém-se no documento.

### Emissão, conteúdo e reimpressão

Para uma doação paga, a emissão deve determinar emitente e série, atribuir sequencial único, guardar uma fotografia fiscal imutável, calcular e guardar assinatura, ATCUD e conteúdo do QR, gerar o PDF final e permitir a entrega existente. Repetições de pedidos, webhooks ou emails não podem criar um segundo documento. O documento deve conter identificação fiscal do emitente e do doador aplicável, tipo/série/número, data fiscal, valor, IVA ou isenção, ATCUD, QR, excerto de assinatura, certificado e estado de anulação/retificação. Reimpressões devem reproduzir exatamente os mesmos dados fiscais e PDF.

### Assinatura, anulação e SAF-T

Cada documento fiscal deve ser assinado segundo as regras da AT. A chave privada pertence ao produtor e não pode ser guardada na base de dados, em configuração, logs ou interfaces. O sistema deve guardar a versão usada e suportar rotação controlada. Não pode apagar, renumerar, alterar silenciosamente ou reutilizar documentos emitidos. Anulações exigem autorização e motivo, preservam original, ATCUD, assinatura e PDF, e constam em SAF-T com o estado aplicável. Operadores autorizados devem exportar SAF-T por período e emitente, a partir da fotografia fiscal imutável.

### Consulta e auditoria

Utilizadores autorizados devem consultar por emitente, série, número, ATCUD, período, doação e estado. A auditoria deve registar configurações fiscais e séries, emissão, download, reimpressão, anulação, retificação, SAF-T, falhas e bloqueios, identificando ator, data/hora, entidade, ação e resultado sem expor segredos.

## Regras multi-tenant

| Estratégia | Emitente e âmbito da série |
|---|---|
| `SingleInvoiceTable` | O tenant é o emitente fiscal, salvo configuração explícita validada pelo CC. |
| `MultipleTablesPerFoodBank` | Cada Banco Alimentar é âmbito fiscal autónomo quando for o emitente legal. |

Em ambas as estratégias, o isolamento aplica-se a consulta, emissão, anulação e SAF-T. A sequência é única no âmbito fiscal da série. A alteração de estratégia não pode misturar séries nem alterar documentos emitidos.

## Tratamento dos campos da branch histórica

| Campo | Decisão |
|---|---|
| `TaxRegistrationNumber` | Manter como parte do perfil fiscal do emitente. |
| `SoftwareCertificateNumber` | Manter como referência configurável e guardar o valor aplicado em cada documento. |
| `HashCypher` | Não reutilizar para armazenar chaves privadas ou segredos; usar referência segura e versão da chave. |
| `ATCUD` | Não manter como valor fixo; pertence a cada documento. |

## Recibos históricos e requisitos não funcionais

Recibos anteriores à ativação fiscal permanecem disponíveis sem alteração e são claramente distinguidos dos documentos certificados. A implementação deve garantir conformidade verificável por testes, consistência sob concorrência e repetição, imutabilidade, controlo de acesso e segregação de tenants, proteção de dados e chaves, recuperação auditável de falhas, apresentação portuguesa e QR legível. Ambientes não produtivos não podem emitir documentos com séries de produção. O modo de simulação deve ser *fail closed*: uma falha na deteção do ambiente, na configuração ou na marcação persistente bloqueia a emissão, em vez de produzir um documento ambíguo.

## Critérios de aceitação

1. Uma doação paga com perfil fiscal e série ativa recebe número, ATCUD, assinatura, QR e certificado aplicável.
2. Configuração fiscal ou código de validação incompleto bloqueia emissão de produção com erro acionável.
3. Novo download ou email devolve o mesmo documento fiscal.
4. Emissões concorrentes na mesma série recebem números e ATCUD distintos.
5. Bancos Alimentares distintos em `MultipleTablesPerFoodBank` ficam segregados em configuração, séries, numeração, ATCUD e SAF-T.
6. `SingleInvoiceTable` usa a série ativa do emitente configurado para o tenant.
7. Um documento anulado preserva-se, tem estado e motivo, e é exportado em SAF-T.
8. Uma correção preserva ligação auditável ao original sem reutilizar número, ATCUD ou assinatura.
9. A exportação SAF-T usa os dados fiscais imutáveis do período e emitente selecionados.
10. O QR de produção contém os dados exigidos pela AT e coincide com o documento.
11. Recibos históricos mantêm-se inalterados.
12. Utilizadores sem permissão para outro emitente não conseguem consultar, descarregar, anular ou exportar os seus documentos.
13. Com o modo local explicitamente ativo em `Development`, uma doação de teste elegível pode completar no browser a emissão, download, reimpressão e consulta de um recibo simulado sem acesso à AT, Key Vault, série AT ou perfil fiscal real.
14. Cada artefacto e resposta de entrega de simulação apresenta `SIMULAÇÃO — SEM VALIDADE FISCAL`; o PDF apresenta a marca em todas as páginas, o nome do ficheiro começa por `SIMULACAO-` e o QR não respeita o formato QR fiscal da AT.
15. Um documento simulado não tem ATCUD, assinatura fiscal, hash fiscal, certificado de software nem referência de comunicação AT; a sua identificação sintética não pode ser apresentada como ATCUD.
16. Uma segunda emissão para a mesma doação no modo de simulação devolve o mesmo documento e artefacto, sem criar nova sequência; emissões concorrentes para doações distintas permanecem únicas no respetivo âmbito simulado.
17. O modo não pode ser ativado nem emitir fora de `Development`, mesmo que a configuração o peça. Testes cobrem `Testing`, `Staging`, `PreProduction`, `Production` e um nome de ambiente desconhecido.
18. Documentos simulados estão segregados por tenant e por Banco Alimentar, ficam excluídos do SAF-T oficial e não podem ser promovidos, migrados ou entregues fora de `Development`.
