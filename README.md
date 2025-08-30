# Fotara SDK

## About The SDK

This SDK is a Java-based library packaged as a JAR file, designed to help taxpayers seamlessly integrate with the national e-invoicing system. It provides end-to-end support for secure invoice generation and submission, including:

- Certificate Signing Request (CSR) creation
- Private key management
- QR code generation
- Digital signing
- Secure submission via the reporting API

By automating compliance with e-invoicing regulations, the SDK ensures data integrity, authenticity, and a streamlined integration experience for developers.

## Prerequisites

- .NET 9.0 Runtime/SDK installed on your device
- Permission to read/write from the target storage.
- Active account on Fotara.
- Valid taxpayer config file.
- Standard CSR config

## Contents

- Installation
- How to use

-  Usage
  - -  Generating CSR (Certificate Signing Request) keys.
      -  Validating invoices.
      -  Signing invoices.
      -  Generating QR code.
      -  Submitting clearance.
      -  Submitting reports.

-  Valid taxpayer config files.

- -  Input config file for Generating CSR (Certificate Signing Request) keys.

-  Configuration Management
  -  CSRconfig.json

**Installation**

**Using Git:**

Within the active folder, use the following command to install Java ISTD-SDK.

```$ git clone <https://github.com/InvoiceQ-Interns/istd-offline-csharp.git>```

Then go to the ```istd-offline-csharp.csproj``` and add the needed libraries.
```
<ItemGroup>

<PackageReference Include="Newtonsoft.Json" Version="13.0.3">
<PackageReference Include="BouncyCastle.NetCore" Version="1.9.0">
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0">
<PackageReference Include="Microsoft.Extensions.Logging.Console" Version="8.0.0">

</ItemGroup>
```
## How To Use

The following format will apply for the rest of the commands and functionalities of the ISTD-SDK application.

Use the SDK from the windows powershell in the following format:

```$env:env=”<env>; dotnet run <action> <args>```

### Parameters

**&lt;env&gt;:** an environment indicating the properties of the app.

**&lt;action&gt;:** type of process for a specific action. Check actions list.

**&lt;args&gt;:** the needed input for each process.

### Available Environments

- dev : For development/testing
- sim : Fotara simulation environment
- prod : Fotara production environment

### Examples

```$env:env=”dev”; dotnet run generating-csr-keys “../folder/output/” “../folder/config.json”```

## generating-csr-keys

#### Description

#### This action generates a CSR (Certificate Signing Request) file containing the subjects provided by the user alongside public and private keys in the output folder specified

#### Format

```generating-csr-keys <directory> <config-file>```

#### Args

| **Arg Name** | **Description** | **Example** |
| --- | --- | --- |
| directory | Output path for generated files | /home/orgs/sdk/output |
| config-file | Path to JSON config file | /home/orgs/sdk/config.json |

**Output**

| **File Name** | **Description** | **Location** |
| --- | --- | --- |
| {enName}{creationTime}.csr | Encrypted CSR (Base64) | Output directory |
| {enName}{creationTime}.key | Encrypted private key | Output directory |
| {enName}{creationTime}.pub | Encrypted public key | Output |

#### Usage Example

```$ env:env=”dev”; dotnet run generating-csr-keys “../folder/output/” “../folder/config.json”```

#### Note

#### This action uses a configuration file from resource folder, [check here for more details.

## invoice-validate

#### Description

**This action takes the signed xml invoice and validates it, returning a response containing various validations.**

#### Format

```Invoice-validate <xml-file-path>```

#### Args

| **Arg Name** | **Description** | **Example** |
| --- | --- | --- |
| einvoice-xml-file- path | Path to E-Invoice XML file (UBL 2.1) | home/orgs/sdk/invoice.xml |

**Output (Console)**

- XSD VALIDATION
- CALCULATION VALIDATION
- REGULATION VALIDATION

#### Usage Example

```$env:env=”dev”; dotnet run invoice-validate “../folder/invoice.xml”```

## invoice-sign

**Description:**

**This action takes the unsigned xml alongside a private key, certificate, and the output path, this process generates a QR code and displays it in the console with the hash of that signed invoice.**

#### Format

Invoice-sign <xml-path> <private-key-path> <certificate-path> <output-path>

#### Args

| **Arg Name** | **Description** | **Example** |
| --- | --- | --- |
| xml-path | Path to E-Invoice XML (UBL 2.1) | /home/orgs/sdk/invoice.xml |
| private-key- path | Encrypted private key path | /home/orgs/sdk/output/{enName}<br><br>{creationTime}.key |
| certificate- path | Encrypted certificate path | /orgs/sdk/output/production_csid.cer |
| output-path | Output path for signed XML | /orgs/sdk/output/signed_invoice.xml |

**Output**

| **Output** | **Description** | **Location** |
| --- | --- | --- |
| Invoice Hash | Hash of the invoice | Console/Log |
| QR Code | Generated QR code | Console/Log |
| Signed XML | Final signed invoice XML | Output path |

#### Usage Example

```$env:env=”dev”; dotnet run invoice-sign “../folder/invoice.xml” “../folder/privatekey.key” “../folder/certificate.cer” “../folder/output”```

## generate-qr

**Description:**

**This action takes an invoice with private key and a certificate and generates a QR code with the hash.**

#### Format

```generate-qr <xml-path> <private-key-path> <certificate-path>```

#### Args

| **Arg Name** | **Description** | **Example** |
| --- | --- | --- |
| xml-path | Path to E-Invoice XML (UBL 2.1) | orgs/sdk/invoice.xml |
| private-key- path | Encrypted private key path | sdk/output/{enName}{creationTime}.pem |
| certificate- path | Encrypted certificate path | /home/orgs/sdk/output/production_csid.cer |

**Output**

| **Output** | **Description** | **Location** |
| --- | --- | --- |
| Invoice Hash | Hash of the invoice | Console/Log |
| QR Code | Generated QR code | Console/Log |

#### Usage Example

```$env:env=”dev”; dotnet run generate-qr “../folder/invoice.xml” “../folder/privatekey.key” “../folder/certificate.cer”```

## submit-clearance

**Description:**

**This action takes the client id with the secret key and the signed invoice and submits the invoice and prints a response body.**

#### Format

```submit-clearance <client-id> <secret-key> <signed-xml>```

#### Args

| **Arg Name** | **Description** | **Example** |
| --- | --- | --- |
| client-id | the client’s identification number | "321" |
| secret-key | the client’s password | "123" |
| signed-xml-path | path of the signed invoice | /home/orgs/sdk/signed-invoice.xml |

#### Output

| **Output** | **Description** | **Location** |
| --- | --- | --- |
| Response code | Console response from QPT | Console/Log |
| Response body | A successful invoice submission response indicating the invoice was already submitted previously, with validation passed and QR code generated. | Console/Log |

#### Usage Example

```$env:env=”dev”; dotnet run submit-invoice “123” “321” “../folder/invoice.xml”```

## submit-report

**Description:**

**This action works similarly as the submit-clearance**

#### Format

```submit-report <client-id> <secret-key> <signed-xml>```

#### Args

| **Arg Name** | **Description** | **Example** |
| --- | --- | --- |
| client-id | the clients identification number | "321" |
| secret-key | the clients password | "123" |
| signed-xml- path | path of the signed invoice | /home/orgs/sdk/signed- invoice.xml |

#### Output

| **Output** | **Description** | **Location** |
| --- | --- | --- |
| Response code | Console response from QPT | Console/Log |
| Response body | A successful invoice submission with warnings if any exist | Console/Log |

#### Usage Example

```$env:env=”dev”; dotnet run submit-invoice “123” “321” “../folder/invoice.xml”```

##

## Valid Taxpayer config file

**Description:**

**The Config file for CSR (Certificate Signing Request) config file input must be in following structure:**

## Taxpayer Config File
```
{

  "CommonName":"english name", 
  "organization":"organizationIdentifier",
  "organizationUnitName":"organizationUnitName", 
  "SerialNumber":"serial number",
  "Country(ISO2)":"country code"

}
```
### Details

<table><tbody><tr><th><h3>Field name</h3></th><th><h3>Description</h3></th><th><h3>Input Type</h3></th><th><h3>Required</h3></th></tr><tr><td><h3>CommonName</h3></td><td><h3>The english name</h3></td><td><h3>String (must be inside “ “)</h3></td><td><h3>Yes</h3></td></tr><tr><td><h3>organization</h3></td><td><h3>Organization identifier</h3></td><td><h3>String (must be inside “ “)</h3></td><td><h3>Yes</h3></td></tr><tr><td><h3>organizationUnitName</h3></td><td><h3>Organization Unit Name</h3></td><td><h3>String (must be inside “ “)</h3></td><td><h3>Yes</h3></td></tr><tr><td><h3>SerialNumber</h3></td><td><h3>Serial Number</h3></td><td><h3>String (must be inside “ “)</h3></td><td><h3>Yes</h3></td></tr><tr><td><h3>Country(ISO2)</h3></td><td><h3>Country code in 2 letters</h3></td><td><h3>String (must be inside “ “)</h3></td><td><h3>Yes</h3></td></tr></tbody></table>

### Example

## Valid TaxPayerConfig

{

  "CommonName":"InvoiceQ", 
  "organization":"InvoiceQ123", 
  "organizationUnitName":"IT team", 
  "SerialNumber":"1234567",
  "Country(ISO2)":"JO"

}

## CSR Configuration File

**Description:**

**The Configuration File in the resources file that changes the behavior of the generating CSR (Certificate Signing Request) file**

### the default CSR config file
```
{

  "keySize":2048, 
  
  "templateOid":"1.3.6.1.4.1.311.21.8.3295615.9391522.3558334.2790417.1961463.187.10509973.13081228",
  
  "major":100,

  "minor":26

}
```
### Details

<table><tbody><tr><th><h3>Field name</h3></th><th><h3>Description</h3></th><th><h3>Input Type</h3></th><th><h3>Required</h3></th><th><h3>Minimum</h3></th></tr><tr><td><h3>keySize</h3></td><td><h3>The key size for generating CSR in RSA algorithm</h3></td><td><h3>Number (must not be inside “ ”)</h3></td><td><h3>Yes</h3></td><td><h3>2048</h3></td></tr><tr><td><h3>templateOid</h3></td><td><h3>The template oid used in the extension</h3></td><td><h3>String (must be inside “ “)</h3></td><td><h3>Yes</h3></td><td><h3>Must not be empty and must be a valid OID</h3></td></tr><tr><td><h3>major</h3></td><td><h3>The Major version used in the extension</h3></td><td><h3>Number (must not be inside “ ”)</h3></td><td><h3>Yes</h3></td><td><h3>Must be greater than 0</h3></td></tr><tr><td><h3>minor</h3></td><td><h3>The Minor version used in the extension</h3></td><td><h3>Number (must not be inside “ ”)</h3></td><td><h3>Yes</h3></td><td><h3>Must be greater or equal than 0</h3></td></tr></tbody></table>
