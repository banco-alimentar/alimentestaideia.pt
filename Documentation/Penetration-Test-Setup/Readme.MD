# Penetration testing of the application

## Objective

Objective of this Penetration testing exercise is to uncover potential security vulnerabilities in the application. For the **initial execution** the plan is to execute the tests in an **isolated environment** such as a Virtual Machine **and not** against the actual development or production environment.

## Containerization of the Web Application

Containerizing the application enables easy testing of the application in an isolated environment.

* The [Dockerfile](./Dockerfile) can be used to containerize the application. File needs to be placed in the [BancoAlimentar.AlimentaEstaIdeia.Web](./BancoAlimentar.AlimentaEstaIdeia.Web) folder. Similary copy the .dockerignore file in this folder.
* After the Dockerfile has been placed in the Web folder, we next need to copy the [docker-compose-template.yaml](./docker-compose-template.yaml) file in the root of the repository.
* Next we need to provide the values of the environment variables in this file, and then rename this template file to docker-compose.yaml. Please note that the value of the ${SQL_PASSWORD} environment variable needs to be same for both the web and ms_sql services.
* Assuming that you have docker installed, the containerized application along with the database can then be brought up using the command ```docker-compose up``` in the root of the repository. By default the application can be accessed on localhost:5051 and the database on localhost:1433

## Suggested Setup of Penetration Testing Environment

Isolated penetration tests are best carried out in a VM within so that we can enforce that the traffic is constrained to that VM.

### Penetration testing - Azure VM setup

* Kali Linux has most tools needed for penetration testing pre-installed and it is recommended we create kali linux VM in Azure
* Open ssh and optionally RDP ports for this VM.
  * Install docker, and docker compose on this VM
  * Install xrdp if you want to rdp on to the machine to use tools like owasp zap in GUI mode. If using RDP create a user on the VM, and the credentials for this user can be used in the rdp client application. For RDP it is best that the VM is created in an Azure Region closest to you, with RDP p.
  * Once the VM is setup clone / copy the repository to the VM using the tool of your choice. Place the Dockerfile, .dockerignore and docker-compose files in the folders mentioned above.
  * From the repository root execute the ```docker-compose up``` command to bring up the application and database within the VM. 
* To connect to the application / database running inside the VM, locally from your laptop/desktop, you can create a ssh tunnel like ``` ssh -L 5051:localhost:5051 kalivm ``` or ``` ssh -L 1433:localhost:1433 kalivm ``` (assuming that kalivm is added to the ssh config file with details to connect to the VM). After the tunnel is created the application can be accessed on localhost:5051 and the database on localhost:1433 . Alternately if you dont wish to create a tunnel you can open the web and RDP ports in the Azure NSG, however this option is less secure.
  
## Logging Penetration Testing Issues

Issues should be created for potential vulnerabilities with the label **penetration test**
