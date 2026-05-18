# OfferteTool 

ASP.NET webapplicatie ter ondersteuning van het offertetraject-proces

## Setup gids

### Benodigdheden

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL
- Entity Framework Core CLI:

```powershell
dotnet tool install --global dotnet-ef
```

Als `dotnet-ef` al geinstalleerd is, werk deze dan bij naar een versie die past bij .NET 10:

```powershell
dotnet tool update --global dotnet-ef
```

### Stappen

1. Clone de repository en ga naar de projectmap:

```powershell
git clone <repository-url>
cd OfferteTool
```

2. Zorg dat PostgreSQL lokaal draait. De database zelf wordt aangemaakt wanneer je de migrations toepast.

3. Configureer lokale secrets voor de `Presentation` applicatie. Vervang de connection string waar nodig door je eigen PostgreSQL gegevens:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=OfferteTool;Username=postgres;Password=postgres" --project Presentation
dotnet user-secrets set "TenderSubmissionEncryption:Algorithm" "AES-256-GCM" --project Presentation
```

Maak daarna een lokale encryptiesleutel aan en sla die op als user secret:

```powershell
$encryptionKey = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet user-secrets set "TenderSubmissionEncryption:Key" $encryptionKey --project Presentation
```

4. Configureer SMTP instellingen als je e-mailfunctionaliteit lokaal wilt testen:

```powershell
dotnet user-secrets set "SmtpEmail:Host" "localhost" --project Presentation
dotnet user-secrets set "SmtpEmail:Port" "2525" --project Presentation
dotnet user-secrets set "SmtpEmail:UserName" "<smtp-gebruiker>" --project Presentation
dotnet user-secrets set "SmtpEmail:Password" "<smtp-wachtwoord>" --project Presentation
dotnet user-secrets set "SmtpEmail:EnableSsl" "false" --project Presentation
dotnet user-secrets set "SmtpEmail:FromAddress" "noreply@example.local" --project Presentation
dotnet user-secrets set "SmtpEmail:FromName" "OfferteTool" --project Presentation
```

5. Herstel packages en pas de database migrations toe. Als de database uit de connection string nog niet bestaat, wordt deze hierbij aangemaakt:

```powershell
dotnet restore
dotnet ef database update --project Infrastructure --startup-project Presentation
```

6. Start de applicatie:

```powershell
dotnet run --project Presentation --launch-profile https
```

De applicatie is daarna beschikbaar op `https://localhost:7018` en `http://localhost:5204`. Bij het starten in de Development omgeving worden testgegevens toegevoegd. Je kunt inloggen met onder andere:

- `beheerder@test.nl` / `Password123!`
- `inkoper@test.nl` / `Password123!`
- `beoordelaar@test.nl` / `Password123!`
- `leverancier@test.nl` / `Password123!`

## Versiebeheer methodiek
In deze repository wordt de Gitflow branching strategie gebruikt. Hiervoor worden de onderstaande branches gebruikt:

- `main` - Op deze branch wordt alleen code gezet die klaar is voor productie. Er wordt alleen met deze branch gemerged d.m.v. pull requests. Alle nieuwe toevoegingen aan deze branch zijn voor een release. Alleen de hotfix en release branches maken pull requests naar de main-branch.
- `develop` - Feature-branches worden gemaakt vanuit deze branch en worden na voltooiing met een pull requests hierin gemerged wanneer ze klaar zijn voor het testen.
- `release/[release_nummer]` - Release-branches worden gebruikt ter voorbereiding van nieuwe releases. Deze branch wordt aangemaakt vanuit de develop-branch met alle features die met de nieuwe release mee gaan. Hierna worden er op de release-branch alleen kleine bugfixes en aanpassingen gedaan. Wanneer de release gereed is wordt het d.m.v. een pull request met de main-branch gemerged.
- `feature/[feature_naam]` - Feature-branches worden gebruikt voor alle nieuwe features. De branches worden gemaakt vanuit de develop-branch en wanneer ze gereed zijn met een pull request teruggevoegd.
- `hotfix/[hotfix_naam]` - Deze branch wordt gebruikt wanneer er snel belangrijke veranderingen nodig zijn in de main-branch. Deze veranderingen moeten ook met de develop-branch worden gemerged om te voorkomen dat de bug opnieuw wordt geïntroduceerd.
- `bugfix/[bugfix_naam]` - Bugs met een minder hoge prioriteit worden met deze branches opgelost. In tegenstelling tot hotfixes, wordt bij bugfixes wel het standaard gitflow proces gebruikt. De branches worden dus aangemaakt vanuit develop en d.m.v. een pull request terug gemerged wanneer ze klaar zijn.

Pull requests op de main-, develop- en release-branches worden pas geaccepteerd nadat de pipeline tests een succesvol resultaat teruggeven.

## Mitigatie van bedreigingen

Bedreiging #5 is het risico dat beveiligingsgevoelige acties achteraf niet goed te herleiden zijn, waardoor een gebruiker bijvoorbeeld kan ontkennen dat hij een actie heeft uitgevoerd. Hiervoor is security audit logging toegevoegd. Dit loopt via de ISecurityAuditService, zie regel 5 t/m 8 van ISecurityAuditService.cs. In Login.cshtml.cs worden loginpogingen en lockouts gelogd, en in ServiceExceptionFilter wordt geweigerde toegang gelogd. De audit events worden uiteindelijk door SecurityAuditService opgeslagen in de SecurityAuditLogs tabel in de database.

Bedreiging #6 is het risico op ongeautoriseerde toegang van gebruikers tot gevoelige informatie. Hiervoor is Defense In Depth toegepast in de applicatie door in verschillende lagen de gegevens te beschermen. Zo zie je bij Tenders bijvoorbeeld dat alleen ingelogde gebruikers toegang hebben tot offertetraject pagina's bij regel 12 van de TenderController (via de AuthenticatedControllerBase), daarnaast zie je in het domein object Tender op regel 51 dat de controle over wie de Tender kan beheren bij het domein object zelf zit. Dat wordt op bijvoorbeeld regel 101 van de TenderService gebruikt.

Bedreiging #18 is het risico op overbelasting van het systeem. Hiervoor heb ik ratelimiting toegevoegd in Program.cs. Kwetsbare endpoints, zoals die van het inloggen en toevoegen van offertetrajecten, krijgen een lager limiet. Zie ter illustratie regel 136 van Program.cs

Bedreiging #23 is het risico op Cross Site Request Forgery bij POST-requests. Hiervoor heb ik het attribuut [ValidateAntiForgeryToken] toegevoegd aan controlleracties die POST-requests afhandelen. Zie ter illustratie regel 20 van TenderController.cs.
