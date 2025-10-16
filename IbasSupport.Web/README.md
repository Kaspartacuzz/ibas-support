# IBAS Support (Blazor + Azure Cosmos DB)
*Forfatter: Kasper Jørgensen*

En lille Blazor Web App til at **oprette og se support-henvendelser** for IBAS Cykler. Data lagres i **Azure Cosmos DB (NoSQL)** med category som partition key. 

## Formål 
- Skabe forbindelse mellem en Blazor webapp og Cosmos DB 
- Oprette support-henvendelser via en formular 
- Validering af felter 
- Oversigtsside som henter en liste med alle support-henvendelser 

## Status 
Jeg er kommet i mål med hele opgaven, dog er der et par små fejl og mangler. Der er fx ingen validering på forhandleren. Jeg forstod ikke helt forhandlerens formål i denne process, så har bare gjort det muligt at man selv kan udfylde, om der skal være en forhandler eller ej, samt forhandlerens oplysninger (navn, adresse, tlf.). 

Derudover genereres Id'erne automatisk som tilfældige positive tal. Ideen er her at brugeren ikke skal indtaste nogle id’er, og man ikke skal læse Cosmos for at finde “højeste id + 1”. 

## Opret Cosmos DB (CLI)
```bash
# 1) Log ind i Azure
az login

# 2) Opret resource group (skift navn/region til dine egne værdier)
az group create -n <NavnetPåDinRessourcegruppe> -l <AzureRegion>

# 3) Opret Cosmos DB-konto (NoSQL API)
#    <GlobaltUniktKontoNavn> SKAL være unikt i hele Azure (find på et særpræget navn).
az cosmosdb create -n <GlobaltUniktKontoNavn> -g <NavnetPåDinRessourcegruppe>

# 4) Opret database
az cosmosdb sql database create \
  -a <GlobaltUniktKontoNavn> -g <NavnetPåDinRessourcegruppe> \
  -n <DatabaseNavn>

# 5) Opret container med partition key /category
az cosmosdb sql container create \
  -a <GlobaltUniktKontoNavn> -g <NavnetPåDinRessourcegruppe> \
  -d <DatabaseNavn> -n <ContainerNavn> \
  -p /category --throughput 400

# 6) Hent connection strings (kopiér én af dem til dine user-secrets)
#    (Parameteren --type connection-strings er FAST; du skal ikke indsætte din egen streng her)
az cosmosdb keys list -n <GlobaltUniktKontoNavn> -g <NavnetPåDinRessourcegruppe> --type connection-strings

## Eksempel:

az login
az group create -n rg-ibas-support -l westeurope
az cosmosdb create -n ibas-cosmos-acc-001 -g rg-ibas-support
az cosmosdb sql database create -a ibas-cosmos-acc-001 -g rg-ibas-support -n IBasSupportDB
az cosmosdb sql container create -a ibas-cosmos-acc-001 -g rg-ibas-support -d IBasSupportDB -n ibassupport -p /category --throughput 400
az cosmosdb keys list -n ibas-cosmos-acc-001 -g rg-ibas-support --type connection-strings
