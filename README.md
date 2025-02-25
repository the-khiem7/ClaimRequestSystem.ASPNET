# net04-2025-claimrequest-team1

## Overview

This project is a ClaimRequest application that utilizes Docker for containerization. It consists of an API, a PostgreSQL database, and a PgAdmin interface for database management.

## Prerequisites

- [Docker](https://www.docker.com/get-started) installed on your machine.
- [Docker Compose](https://docs.docker.com/compose/install/) installed.

## Getting Started

Follow these steps to run the application:

1. **Clone the Repository**

   Clone this repository to your local machine using:

   ```bash
   git clone <repository-url>
   cd <repository-directory>
   ```

2. Access to the ClaimRequest.API/appsetting.json
    - first time build the Application:
        - set ApplyMigration: false to true (Enable auto EF Core Migration)
    - next time build the Apllication:
        - set ApplyMigration: true to false (Using existing DB)

3. **Build and Start the Application**

   Run the following command to build and start the application:

   ```bash
   docker-compose up --build
   ```

   This command will:
   - Build the Docker images for the API and database.
   - Start the containers for the API, PostgreSQL database, and PgAdmin.

   Alternative:
   - Using docker-compose run option in Visual Studio to Run 

4. **Access the Application**

   - The API will be available at `http://localhost:5000`.
   - PgAdmin can be accessed at `http://localhost:5050`. Use the following credentials to log in:
     - **Email:** admin@admin.com
     - **Password:** admin

5. **Database Connection**

   The API connects to the PostgreSQL database using the following connection string:

   ```
   Host=claimrequest.db;Database=ClaimRequestDB;Username=db_user;Password=Iloveyou3000!;Port=5432
   ```

## Stopping the Application

To stop the application, press `CTRL + C` in the terminal where the `docker-compose up` command is running. To remove the containers, run:

```bash
docker-compose down
```

## Troubleshooting

- If you encounter issues with the database connection, ensure that the database container is healthy and running.
- Check the logs of the containers for any error messages using:

```bash
docker-compose logs
```

