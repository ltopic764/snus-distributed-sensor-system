# snus-distributed-sensor-system
Repozitorijum projekta iz predmeta nadzorno-upravljački sistemi.

Distribuirani sistem za prikupljanje, obradu i čuvanje podataka sa senzora
temperature. Primer primene: nadzor temperature u jezgru nuklearne elektrane.
Sistem prati vrednosti i alarme, tolerantan je na otkaze, računa konsenzus
pouzdanih senzora i koristi šifrovanu, potpisanu komunikaciju.

## Arhitektura

Sistem se sastoji od šest komponenti:

- **Shared** — zajednički modeli, DTO-ovi, enumeracije i kripto helper
- **IngestionService** — REST API koji prima podatke senzora, čuva ih u bazu,
  ispisuje alarme i obaveštava NotificationService
- **ConsensusService** — worker koji svakog minuta računa BFT konsenzus
- **NotificationService** — SignalR hub za alarme u realnom vremenu
- **SensorClient** — simulacija senzora (šifruje i potpisuje poruke)
- **Ingress** — jedinstvena ulazna tačka

Baza: PostgreSQL (Entity Framework Core).

## Preduslovi

- Docker Desktop
- (opciono, za razvoj) .NET 8 SDK
- (opciono, za Kubernetes demo) Minikube

## Pokretanje (Docker Compose)

U root folderu:

    docker compose up --build

Prvi put traje duže jer se builduju svi kontejneri. Kada se podigne, svih šest
komponenti radi. Zaustavljanje:

    docker compose down          # gasi kontejnere, baza ostaje
    docker compose down -v       # gasi i briše bazu

## Šta se vidi kad radi

- **SensorClient** ispisuje merenja u boji; alarmi žuto/narandžasto/crveno
- **IngestionService** ispisuje primljene alarme u boji
- **ConsensusService** svakog minuta ispisuje izračunatu konsenzus vrednost
- **Ingress** je na `http://localhost:5115`

Rute kroz Ingress:

- `POST /api/ingest` — prijem merenja (koriste senzori)
- `GET  /api/reports/readings` — istorijska merenja
- `GET  /api/reports/alarms` — evidencija alarma
- `GET  /api/reports/consensus` — konsenzus vrednosti
- `/hub` — SignalR za realtime alarme

## Demonstracija na dva računara

Server na jednom, senzori na drugom (ista lokalna mreža):

1. Na računaru sa serverom saznati lokalnu IP adresu (`ipconfig`) i otvoriti
   port na firewall-u.
2. Prekopirati `keys/server_public.pem` na računar sa senzorima.
3. Na računaru sa senzorima podesiti `IngressBaseUrl` na adresu servera
   (npr. `http://192.168.1.20:5115/`) i pokrenuti SensorClient.
