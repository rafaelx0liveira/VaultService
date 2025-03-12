# **VaultService - Library for HashiCorp Vault Integration**

## 🚀 **Introduction**
The **VaultService** library is optimized to facilitate integration with **HashiCorp Vault**, providing:
- **In-memory storage** to avoid unnecessary API calls to Vault.
- **On-demand fetching** without needing to load all secrets at startup.
- **Support for subfolders in Vault**, allowing retrieval of secrets organized in directories.
- **Simple and intuitive API**, reducing implementation complexity.
- **High performance**, eliminating repetitive Vault calls and improving application responsiveness.
- **Thread-safe caching using `ConcurrentDictionary`** for improved multi-threading safety.
- **Automatic Vault connection verification** upon initialization.

> 📌 **Differentiator:** The default VaultClient consumes the Vault API **every time a secret is requested**, which can lead to **high latency and performance impact**. This library solves this problem by storing secrets in **memory cache**, avoiding unnecessary calls.

---

## 📦 **Installation**

To install via NuGet:
```sh
 dotnet add package VaultService
```
Or manually add it to `.csproj`:
```xml
<ItemGroup>
    <PackageReference Include="VaultService" Version="1.0.0" />
</ItemGroup>
```

---

## 📌 **Dependencies**
This library requires the following dependencies:
- [**VaultSharp**](https://github.com/rajanadar/VaultSharp) - Official client for HashiCorp Vault.
- [**Serilog**](https://serilog.net/) - For structured logging and log handling in console and files.
- **ConcurrentDictionary** - Used for thread-safe caching.

To ensure proper functionality, install these dependencies:
```sh
dotnet add package VaultSharp
dotnet add package Serilog
dotnet add package Serilog.Sinks.Console
```

---

## 🔧 **Initial Configuration**

### **1️⃣ Register in `Program.cs`**
```csharp
using VaultService.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Configure VaultService
builder.Services.AddVaultService(
    vaultAddress: "http://localhost:8200/", // 🔥 Your Vault address
    vaultToken: "your-token",                 // 🔥 Your Vault authentication token
    mountPoint: "secret",                      // 🔥 Your Vault mount point
    basePath: "project"                        // 🔥 Your Vault base path
);

var app = builder.Build();
app.Run();
```

---

## 🎯 **Library Usage**
### **🔹 Fetch Secret Easily**

```csharp
public class MyService
{
    private readonly IVaultClient _vaultClient;
    
    public MyService(IVaultClient vaultClient)
    {
        _vaultClient = vaultClient;
    }

    public void RetrieveSecret()
    {
        var jwtSecret = _vaultClient.GetSecret("project/database:JwtSecret");
        Console.WriteLine($"🔐 JwtSecret: {jwtSecret}");
    }
}
```

### **🔹 Alternatively, pass `path` and `key` separately**
```csharp
var rabbitHost = keyVaultService.GetSecret("project/rabbitmq/configs", "HostName");
```

---

## ⚡ **VaultService Advantages**

### **✅ 1. Stores Secrets in Memory with Thread Safety**
🔹 Unlike the default `VaultClient`, which makes API requests **every time a secret is requested**, this library **caches secrets in memory** using a **thread-safe `ConcurrentDictionary`**. 

- 🚀 **First call**: Fetches the secret from Vault and stores it in RAM.
- ⚡ **Subsequent calls**: Instantly returns from memory.

```csharp
var jwtSecret = _vaultClient.GetSecret("project/database:JwtSecret"); // 🔥 First call: fetches from Vault
var jwtSecret2 = _vaultClient.GetSecret("project/database:JwtSecret"); // ⚡ Second call: fetches from memory
```

### **✅ 2. Supports Vault Subfolders**
🔹 HashiCorp Vault allows secrets to be stored in directories, and this library automatically resolves **subfolder searches**.

Example Vault structure:
```
secret/project/
  ├── rabbitmq/
  │   ├── configs/
  │   │   ├── hostname: "rabbit-host"
  │   │   ├── username: "rabbit-user"
  │   │   ├── password: "rabbit-pass"
  ├── jwt/
  │   ├── secret: "my-jwt-secret"
```
Now, fetching secrets from **subfolders** is easy:
```csharp
var rabbitUser = _vaultClient.GetSecret("project/rabbitmq/configs:username");
var jwtSecret = _vaultClient.GetSecret("project/jwt:secret");
```

### **✅ 3. On-Demand Fetching for Better Performance**
🔹 Avoids loading all secrets at startup, fetching only when needed.
```csharp
var apiKey = _vaultClient.GetSecret("project/api:ApiKey");
```

### **✅ 4. Automatic Vault Connection Verification**
🔹 Ensures that Vault is accessible **at connection time**:
```csharp
var healthStatus = _vaultClient.V1.System.GetHealthStatusAsync().Result;
if (healthStatus.Sealed)
{
    throw new VaultServiceException("Vault is sealed and cannot be accessed.");
}
```

### **✅ 5. Improved Error Handling with `VaultServiceException`**
🔹 If a secret is not found, a friendly error will be thrown:
```csharp
try
{
    var invalidSecret = _vaultClient.GetSecret("project/database:InvalidKey");
}
catch (VaultServiceException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

---

## 🔥 **Conclusion**

The **VaultService** is the ideal solution for efficient integration with HashiCorp Vault, offering **performance, caching, and ease of use**. 

✅ **In-Memory Caching for High Performance**  
✅ **Smart Secret Fetching without Excessive Requests**  
✅ **Support for Hierarchical Secret Structure**  
✅ **Automatic Vault Connection Verification**  
✅ **Easy Configuration and Usage**  

**📦 Install now and optimize your application!** 🚀

