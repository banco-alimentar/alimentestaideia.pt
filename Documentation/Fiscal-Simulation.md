# Simulação fiscal local

## Finalidade

O modo de simulação permite confirmar, no navegador e em `Development`, o percurso de emissão e entrega de um recibo para uma doação paga que pede recibo. Produz deliberadamente um artefacto separado, com o aviso obrigatório **SIMULAÇÃO — SEM VALIDADE FISCAL**.

Não produz um documento fiscal: não comunica com a AT, não usa certificado ou Azure Key Vault, não requer uma série ATCUD, não gera um QR fiscal da AT e nunca pode integrar um ficheiro SAF-T oficial.

## Ativar localmente

Não altere ficheiros `appsettings` partilhados. Na raiz do repositório, configure User Secrets para o projeto Web:

```bash
dotnet user-secrets set "Fiscal:Simulation:Enabled" "true" --project BancoAlimentar.AlimentaEstaIdeia.Web/BancoAlimentar.AlimentaEstaIdeia.Web.csproj
```

Em alternativa, defina a variável de ambiente `Fiscal__Simulation__Enabled=true` somente no processo local. O ambiente tem de ser exatamente `Development`; se esta opção estiver ativa em `Staging`, `PreProduction`, `Production` ou qualquer outro nome, a aplicação não arranca. Desative-a removendo o segredo ou definindo o valor como `false`.

O processo continua a precisar da configuração Azure Storage de desenvolvimento já usada pela aplicação (`AzureStorage:ConnectionString` e `AzureStorage:PdfContainerName` ou `AzureStorage:FiscalPdfContainerName`). O PDF é guardado de forma imutável abaixo de `simulation/development/...`, separado de `fiscal-receipts/...`.

## Teste no navegador

1. Arranque a aplicação local com `ASPNETCORE_ENVIRONMENT=Development`.
2. Use uma doação de teste com pagamento confirmado e a opção de recibo ativa.
3. Autentique-se como o utilizador da doação, ou como administrador, e abra o botão de recibo no histórico/detalhe da doação. O endpoint é `/Identity/Account/Manage/GenerateInvoice?publicDonationId=<id-público-da-doação>`.
4. O navegador deve abrir um PDF cujo nome começa por `SIMULACAO-`. O PDF deve apresentar o aviso em destaque e um QR privado de simulação.
5. Abra novamente o mesmo recibo. Deve ser recarregado o mesmo PDF persistido, sem criar um novo identificador.

Teste ambos os tenants/estratégias quando aplicável: `SingleInvoiceTable` atribui a sequência simulada ao tenant; `MultipleTablesPerFoodBank` atribui-a ao tenant e banco alimentar da doação.

## Limites de segurança

Esta funcionalidade não substitui o teste de integração AT nem a certificação do programa. Nunca a ative fora de `Development`, nunca entregue estes PDFs a doadores como comprovativo fiscal e nunca exporte os seus registos para SAF-T oficial.
