# IPCalci

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![NuGet](https://img.shields.io/nuget/v/IPCalci.svg)](https://www.nuget.org/packages/IPCalci/)

A high-performance, developer-friendly .NET library for parsing, validating, and calculating IPv4 and IPv6 subnet metrics, network addresses, usable host ranges, and CIDR notations.

---

## Features

- **IPv4 & IPv6 Support**: Comprehensive calculations for both IPv4 and IPv6 addresses.
- **Dedicated & Auto-Detect Parsers**: Explicit `parseIPv4` / `parseIPv6` methods or smart auto-routing `parse`.
- **Rich Dictionary Output**: Key-value dictionary with case-insensitive key lookups and convenient aliases.
- **Detailed Subnet Metrics**:
  - Network address (compressed & expanded formats)
  - Subnet mask & prefix netmask
  - CIDR notation & prefix length
  - First and last usable host IPs
  - Total and usable host counts (using `BigInteger` for accurate 128-bit IPv6 numbers)
  - IP scope / classification (Public, Private, Loopback, Link-Local, ULA, Multicast, CGNAT, etc.)
  - Binary and hexadecimal representations
  - IPv6 reverse DNS pointer (`.ip6.arpa`)
- **Special Subnet Awareness**: RFC 3021 (`/31` point-to-point), RFC 6164 (`/127` IPv6 inter-router), host routes (`/32` & `/128`).
- **Zero Third-Party Runtime Dependencies**: Built directly on modern .NET primitives.

---

## Installation

Install via the .NET CLI:

```bash
dotnet add package IPCalci
```

Or via the NuGet Package Manager Console:

```powershell
Install-Package IPCalci
```

---

## Quick Start

```csharp
using IPCalci;

// 1. IPv4 Parsing
var ipv4 = IPCalci.parseIPv4("192.168.1.50/24");

Console.WriteLine(ipv4["Network"]);           // "192.168.1.0"
Console.WriteLine(ipv4["Subnet Mask"]);       // "255.255.255.0"
Console.WriteLine(ipv4["CIDR Notation"]);     // "/24"
Console.WriteLine(ipv4["First Usable IP"]);   // "192.168.1.1"
Console.WriteLine(ipv4["Last Usable IP"]);    // "192.168.1.254"
Console.WriteLine(ipv4["Broadcast Address"]); // "192.168.1.255"
Console.WriteLine(ipv4["Usable Hosts"]);      // "254"
Console.WriteLine(ipv4["IP Type"]);           // "Private (RFC 1918)"

// 2. IPv6 Parsing
var ipv6 = IPCalci.parseIPv6("2001:db8:85a3::8a2e:370:7334/64");

Console.WriteLine(ipv6["Network"]);               // "2001:db8:85a3::"
Console.WriteLine(ipv6["Expanded IP Address"]);   // "2001:0db8:85a3:0000:0000:8a2e:0370:7334"
Console.WriteLine(ipv6["Prefix Netmask"]);        // "ffff:ffff:ffff:ffff:0000:0000:0000:0000"
Console.WriteLine(ipv6["First Usable IP"]);       // "2001:db8:85a3::1"
Console.WriteLine(ipv6["Last Usable IP"]);        // "2001:db8:85a3:0:ffff:ffff:ffff:ffff"
Console.WriteLine(ipv6["Total Number of Hosts"]); // "18446744073709551616"
Console.WriteLine(ipv6["Reverse DNS Pointer"]);   // "4.3.3.7.0.7.3.0.e.2.a.8.0.0.0.0.0.0.0.0.3.a.5.8.8.b.d.0.1.0.0.2.ip6.arpa"

// 3. Unified Auto-detect Parser (works for both IPv4 and IPv6)
var autoIpv4 = IPCalci.parse("10.0.0.1/8");
var autoIpv6 = IPCalci.parse("fe80::1/64");
```

---

## Flexible Input Formats

- **CIDR notation**: `"192.168.1.1/24"`, `"2001:db8::1/64"`
- **Dotted-decimal subnet mask**: `"192.168.1.1/255.255.255.0"` or `"192.168.1.1 255.255.255.0"`
- **Bracketed IPv6**: `"[2001:db8::1]/64"`
- **Separate parameters**: `parseIPv4("192.168.1.1", "255.255.255.0")`, `parseIPv6("2001:db8::1", 64)`
- **Plain IP without mask**: Automatically infers standard defaults (Class A/B/C for IPv4, `/64` for IPv6).

---

## Dictionary Keys Reference

Keys are **case-insensitive** (`result["network"]` == `result["Network"]`).

### IPv4 Keys
- `Network`, `Network Address`
- `Subnet Mask`, `SubnetMask`
- `CIDR Notation`, `CIDR`
- `First Usable IP`, `FirstUsableIp`
- `Broadcast Address`, `Broadcast`
- `Last Usable IP`, `LastUsableIp`
- `Usable Host Range`, `UsableHostRange`
- `Total Number of Hosts`, `Total Hosts`
- `Number of Usable Hosts`, `Usable Hosts`
- `Wildcard Mask`, `Wildcard`
- `IP Class`, `Class`
- `IP Type`, `Type`
- `IP Address`, `IP`
- `CIDR Network`
- `Binary IP Address`, `Binary Subnet Mask`
- `Hex IP Address`, `Decimal IP Address`

### IPv6 Keys
- `IP Address`, `IP`
- `Expanded IP Address`
- `Network`, `Network Address`
- `Expanded Network`
- `CIDR Notation`, `CIDR`
- `Prefix Length`
- `Prefix Netmask`, `Subnet Mask`
- `CIDR Network`
- `First Usable IP`, `FirstUsableIp`
- `Last Usable IP`, `LastUsableIp`
- `Usable Host Range`
- `Total Number of Hosts`, `Total Hosts`
- `Number of Usable Hosts`, `Usable Hosts`
- `IP Type`, `Type`
- `Reverse DNS Pointer`, `Reverse DNS`
- `Binary IP Address`, `Hex IP Address`

---

## License

This project is licensed under the MIT License.

```text
MIT License

Copyright (c) 2026 Kasali

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

