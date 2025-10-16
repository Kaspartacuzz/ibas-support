# IBAS Support (Blazor + Azure Cosmos DB)
*Forfatter: Kasper Jørgensen*           

En lille Blazor Web App til at **oprette og se support-henvendelser** for IBAS Cykler. Data lagres i **Azure Cosmos DB (NoSQL)** med category som partition key. 

## Formål
- Skabe forbindelse mellem en Blazor webapp og Cosmos DB
- Oprette support-henvendelser via en formular
- Validering af felter         
- Oversigtsside som henter en liste med alle support-henvendelser

## Status                               
Jeg er kommet i mål med hele opgaven, dog er der et par små fejl og mangler. Der er fx ingen validering på forhandleren. Jeg forstod ikke helt forhandlerens formål i denne process, så har bare gjort det muligt at man selv kan udfylde, om der skal være en forhandler eller ej, endvidere med forhandlerens oplysninger (navn, adresse, tlf.).

Derudover genereres Id'erne automatisk som tilfældige positive tal. Ideen er her at brugeren ikke skal indtaste nogle id’er, og man ikke skal læse Cosmos DB for at finde “højeste eksisterende id + 1”.  

## Opret Cosmos DB (CLI)
```bash                                 
# 1) Log ind i Azure             
az login                                                      
  
# 2) Opret resource group (skift navn/region til de korrekte værdier)
az group create -n <NavnetPåDinRessourcegruppe> -l <AzureRegion>

# 3) Opret Cosmos DB-konto (NoSQL API)                        
#    <KontoNavn> SKAL være unikt i hele Azure (find på et særpræget navn).                         
az cosmosdb create -n <KontoNavn> -g <NavnetPåRessourcegruppe>

# 4) Opret database                     
az cosmosdb sql database create \
  -a <KontoNavn> -g <NavnetPåRessourcegruppe> \
  -n <DatabaseNavn>

# 5) Opret container med partition key /category                
az cosmosdb sql container create \
  -a <KontoNavn> -g <NavnetPåRessourcegruppe> \
  -d <DatabaseNavn> -n <ContainerNavn> \
  -p /category --throughput 400

# 6) Hent connection strings (kopiér én af dem til dine user-secrets)
#    (Parameteren --type connection-strings er FAST; du skal ikke indsætte din egen streng her)                                                 
az cosmosdb keys list -n <KontoNavn> -g <Ressourcegruppe> --type connection-strings                                    
  
## Eksempel for denne opgave:                   
az login
az group create -n IBasSupportRG -l swedencentral
az cosmosdb create -n ibas-db-account-3841 -g IBasSupportRG
az cosmosdb sql database create -a ibas-db-account-3841 -g IBasSupportRG -n IBasSupportDB
az cosmosdb sql container create -a ibas-db-account-3841 -g IBasSupportRG -d IBasSupportDB -n ibassupport -p /category --throughput 400
az cosmosdb keys list -n ibas-db-account-3841 -g IBasSupportRG --type connection-strings
