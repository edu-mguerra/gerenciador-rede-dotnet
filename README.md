#  Gerenciador de Rede Híbrido (Network Guardian API)

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=csharp&logoColor=white)
![MySQL](https://img.shields.io/badge/MySQL-4479A1?style=for-the-badge&logo=mysql&logoColor=white)
![Swagger](https://img.shields.io/badge/Swagger-85EA2D?style=for-the-badge&logo=swagger&logoColor=black)

Uma API REST de alta performance desenvolvida em **.NET 8** e **C#** focada em infraestrutura de redes e segurança cibernética. Este ecossistema une a performance da captura de pacotes em baixo nível com a confiabilidade de uma arquitetura web moderna, permitindo mapear a topologia local, auditar tráfego e gerenciar estados de dispositivos em tempo real.

---

## ✨ Funcionalidades Principais

* **> Scanner de Rede Assíncrono (Camada 2):** Varreduras contínuas em background via ARP para descobrir e atualizar dispositivos ativos na sub-rede, registrando o momento exato em que um aparelho entra ou muda de IP.
* **> Sniffer de Tráfego Inteligente:** Captura de pacotes brutos IPv4 em modo promíscuo, filtrando requisições críticas de navegação (portas 80 HTTP, 443 HTTPS e 53 DNS).
* **> Controlador de Acesso (ARP Spoofing):** Bloqueio e isolamento de dispositivos da rede de forma programática através da injeção de pacotes ARP falsificados.
* **> Gestão de Estados no Banco de Dados:** Centralização de regras de negócio em banco relacional, registrando status como `Conectou`, `Desconectou` e `BloqueadoPeloAdmin`.

---

## 🛠️ Stack Tecnológica

* **Backend:** C# / .NET 8 (Web API)
* **Manipulação de Rede:** [SharpPcap](https://github.com/dotpcap/sharppcap) e PacketDotNet (WinPcap/Npcap)
* **ORM:** Entity Framework Core (Code-First Migrations)
* **Banco de Dados:** MySQL Server
* **Documentação:** Swagger / OpenAPI

---

## 🏗️ Arquitetura e Engenharia

Este projeto resolve desafios complexos de concorrência e manipulação de baixo nível:
* **Isolamento de Escopo:** Uso de `IServiceScopeFactory` em *BackgroundServices* para garantir thread-safety do DbContext ao processar milhares de pacotes simultâneos.
* **Cache em Memória (Throttle):** Implementação de `ConcurrentDictionary` para agrupar pacotes e proteger o MySQL de exaustão de conexões durante picos de tráfego.
* **Integridade Referencial:** A telemetria é estritamente atrelada aos UUIDs (Guid) dos dispositivos, gerando uma linha do tempo auditável e limpa.

---

## 🚀 Como Executar o Projeto

### Pré-requisitos
1. **.NET 8 SDK** instalado.
2. **MySQL Server** rodando localmente ou em container.
3. **Npcap** ou **WinPcap** instalado no sistema operacional (necessário para o SharpPcap acessar a placa de rede).

### 1. Configurar o Banco de Dados

1. Na raiz do projeto (onde está o `Program.cs`), crie um arquivo chamado `appsettings.json`.
2. Adicione a estrutura abaixo, colocando a sua senha do MySQL:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=GerenciadorRedeDB;Uid=root;Pwd=SUA_SENHA_AQUI"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
