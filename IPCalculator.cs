using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;

namespace IPCalci;

/// <summary>
/// Provides calculation and parsing methods for IPv4 and IPv6 addresses and subnets.
/// </summary>
public class IPCalci
{
    // ==========================================
    // Unified Parse Methods (Auto-detect IP version)
    // ==========================================

    /// <summary>
    /// Parses an IPv4 or IPv6 address string and returns a dictionary of network and subnet calculation details.
    /// Automatically detects whether the input is IPv4 or IPv6.
    /// </summary>
    /// <param name="ip">The IP address in CIDR format (e.g. "192.168.1.1/24" or "2001:db8::1/64") or plain IP.</param>
    /// <returns>A dictionary of key-value pairs with all calculation results.</returns>
    public static Dictionary<string, string> parse(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            throw new ArgumentException("IP address cannot be null, empty, or whitespace.", nameof(ip));
        }

        string trimmed = ip.Trim();
        if (trimmed.Contains(':'))
        {
            return parseIPv6(trimmed);
        }

        return parseIPv4(trimmed);
    }

    /// <summary>
    /// PascalCase alias for <see cref="parse(string)"/>.
    /// </summary>
    public static Dictionary<string, string> Parse(string ip) => parse(ip);

    /// <summary>
    /// Parses an IPv4 or IPv6 address with a separate subnet mask or prefix length.
    /// </summary>
    public static Dictionary<string, string> parse(string ip, string subnetOrPrefix)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            throw new ArgumentException("IP address cannot be null, empty, or whitespace.", nameof(ip));
        }

        if (ip.Contains(':'))
        {
            if (int.TryParse(subnetOrPrefix.Trim().TrimStart('/'), NumberStyles.Integer, CultureInfo.InvariantCulture, out int prefix))
            {
                return parseIPv6(ip, prefix);
            }
            throw new FormatException($"Invalid IPv6 prefix length '{subnetOrPrefix}'. Must be an integer between 0 and 128.");
        }

        return parseIPv4(ip, subnetOrPrefix);
    }

    /// <summary>
    /// PascalCase alias for <see cref="parse(string, string)"/>.
    /// </summary>
    public static Dictionary<string, string> Parse(string ip, string subnetOrPrefix) => parse(ip, subnetOrPrefix);

    // ==========================================
    // IPv4 Specific Parse Methods
    // ==========================================

    /// <summary>
    /// Parses an IPv4 address string (e.g. "192.168.1.10/24", "192.168.1.10 255.255.255.0", or "192.168.1.10")
    /// and returns a dictionary of IPv4 network details.
    /// </summary>
    public static Dictionary<string, string> parseIPv4(string ip) => ParseIPv4Info(ip).ToDictionary();

    /// <summary>
    /// PascalCase alias for <see cref="parseIPv4(string)"/>.
    /// </summary>
    public static Dictionary<string, string> ParseIPv4(string ip) => parseIPv4(ip);

    /// <summary>
    /// Case-insensitive aliases for <see cref="parseIPv4(string)"/>.
    /// </summary>
    public static Dictionary<string, string> parseIpv4(string ip) => parseIPv4(ip);
    public static Dictionary<string, string> ParseIpv4(string ip) => parseIPv4(ip);

    /// <summary>
    /// Parses an IPv4 address with a separate subnet mask or CIDR prefix length.
    /// </summary>
    public static Dictionary<string, string> parseIPv4(string ip, string subnetMaskOrCidr) => ParseIPv4Info(ip, subnetMaskOrCidr).ToDictionary();

    /// <summary>
    /// PascalCase alias for <see cref="parseIPv4(string, string)"/>.
    /// </summary>
    public static Dictionary<string, string> ParseIPv4(string ip, string subnetMaskOrCidr) => parseIPv4(ip, subnetMaskOrCidr);

    /// <summary>
    /// Case-insensitive aliases for <see cref="parseIPv4(string, string)"/>.
    /// </summary>
    public static Dictionary<string, string> parseIpv4(string ip, string subnetMaskOrCidr) => parseIPv4(ip, subnetMaskOrCidr);
    public static Dictionary<string, string> ParseIpv4(string ip, string subnetMaskOrCidr) => parseIPv4(ip, subnetMaskOrCidr);

    /// <summary>
    /// Helper method to return IPv4 results as a list of strings if needed.
    /// </summary>
    public static List<string> ParseToList(string ip, bool withLabels = true) => ParseIPv4Info(ip).ToList(withLabels);

    /// <summary>
    /// Parses an IPv4 address and returns a strongly-typed <see cref="IPv4Info"/> object.
    /// </summary>
    public static IPv4Info ParseInfo(string ip) => ParseIPv4Info(ip);

    /// <summary>
    /// Explicit IPv4 parser returning strongly-typed <see cref="IPv4Info"/>.
    /// </summary>
    public static IPv4Info ParseIPv4Info(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            throw new ArgumentException("IP address cannot be null, empty, or whitespace.", nameof(ip));
        }

        string trimmed = ip.Trim();
        string ipPart;
        string? maskPart = null;

        if (trimmed.Contains('/'))
        {
            var parts = trimmed.Split('/', StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[1]))
            {
                throw new FormatException($"Invalid CIDR format '{ip}'. Expected format: 'IP/CIDR' (e.g. '192.168.1.10/24').");
            }
            ipPart = parts[0];
            maskPart = parts[1];
        }
        else if (trimmed.Contains(' '))
        {
            var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                throw new FormatException($"Invalid IP format '{ip}'. Expected 'IP SubnetMask' or 'IP CIDR'.");
            }
            ipPart = parts[0];
            maskPart = parts[1];
        }
        else
        {
            ipPart = trimmed;
            maskPart = null;
        }

        return ParseIPv4Info(ipPart, maskPart);
    }

    /// <summary>
    /// Parses an IPv4 address and optional subnet mask / CIDR into a strongly-typed <see cref="IPv4Info"/> object.
    /// </summary>
    public static IPv4Info ParseIPv4Info(string ipPart, string? maskPart)
    {
        if (string.IsNullOrWhiteSpace(ipPart))
        {
            throw new ArgumentException("IP address cannot be null, empty, or whitespace.", nameof(ipPart));
        }

        byte[] ipOctets = ParseOctets(ipPart.Trim(), "IPv4 address");
        uint ipUint = OctetsToUint(ipOctets);

        int cidr;
        if (string.IsNullOrWhiteSpace(maskPart))
        {
            cidr = GetDefaultCidr(ipOctets[0]);
        }
        else
        {
            string cleanMask = maskPart.Trim().TrimStart('/');
            if (int.TryParse(cleanMask, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedCidr))
            {
                if (parsedCidr < 0 || parsedCidr > 32)
                {
                    throw new ArgumentOutOfRangeException(nameof(maskPart), $"CIDR prefix length /{parsedCidr} is invalid. Must be between 0 and 32.");
                }
                cidr = parsedCidr;
            }
            else
            {
                byte[] maskOctets = ParseOctets(cleanMask, "Subnet mask");
                uint maskUintFromOctets = OctetsToUint(maskOctets);

                int leadingOnes = BitOperations.LeadingZeroCount(~maskUintFromOctets);
                uint expectedMask = leadingOnes == 0 ? 0U : (0xFFFFFFFFU << (32 - leadingOnes));
                if (expectedMask != maskUintFromOctets)
                {
                    throw new ArgumentException($"Subnet mask '{cleanMask}' is not a valid contiguous netmask.", nameof(maskPart));
                }
                cidr = leadingOnes;
            }
        }

        uint maskUint = cidr == 0 ? 0U : (0xFFFFFFFFU << (32 - cidr));
        uint wildcardUint = ~maskUint;
        uint networkUint = ipUint & maskUint;
        uint broadcastUint = networkUint | wildcardUint;

        string ipStr = UintToIp(ipUint);
        string networkStr = UintToIp(networkUint);
        string maskStr = UintToIp(maskUint);
        string wildcardStr = UintToIp(wildcardUint);
        string broadcastStr = UintToIp(broadcastUint);

        long totalHosts = cidr == 0 ? 4294967296L : (1L << (32 - cidr));

        string firstUsableStr;
        string lastUsableStr;
        string usableRangeStr;
        long usableHosts;

        if (cidr == 32)
        {
            firstUsableStr = ipStr;
            lastUsableStr = ipStr;
            usableRangeStr = ipStr;
            usableHosts = 1;
        }
        else if (cidr == 31)
        {
            firstUsableStr = networkStr;
            lastUsableStr = broadcastStr;
            usableRangeStr = $"{firstUsableStr} - {lastUsableStr}";
            usableHosts = 2;
        }
        else
        {
            firstUsableStr = UintToIp(networkUint + 1);
            lastUsableStr = UintToIp(broadcastUint - 1);
            usableRangeStr = $"{firstUsableStr} - {lastUsableStr}";
            usableHosts = Math.Max(0, totalHosts - 2);
        }

        string ipClass = GetIpClass(ipOctets[0]);
        string ipType = GetIpType(ipOctets);

        return new IPv4Info
        {
            IpAddress = ipStr,
            NetworkAddress = networkStr,
            SubnetMask = maskStr,
            Cidr = cidr,
            FirstUsableIp = firstUsableStr,
            LastUsableIp = lastUsableStr,
            BroadcastAddress = broadcastStr,
            UsableHostRange = usableRangeStr,
            TotalHosts = totalHosts,
            UsableHosts = usableHosts,
            WildcardMask = wildcardStr,
            IpClass = ipClass,
            IpType = ipType,
            BinaryIp = ToBinaryDotted(ipUint),
            BinarySubnetMask = ToBinaryDotted(maskUint),
            HexIp = $"0x{ipUint:X8}",
            DecimalIp = ipUint
        };
    }

    // ==========================================
    // IPv6 Specific Parse Methods
    // ==========================================

    /// <summary>
    /// Parses an IPv6 address string (e.g. "2001:db8::1/64" or "[2001:db8::1]/64")
    /// and returns a dictionary of IPv6 network details.
    /// </summary>
    public static Dictionary<string, string> parseIPv6(string ip) => ParseIPv6Info(ip).ToDictionary();

    /// <summary>
    /// PascalCase alias for <see cref="parseIPv6(string)"/>.
    /// </summary>
    public static Dictionary<string, string> ParseIPv6(string ip) => parseIPv6(ip);

    /// <summary>
    /// Case-insensitive aliases for <see cref="parseIPv6(string)"/>.
    /// </summary>
    public static Dictionary<string, string> parseIpv6(string ip) => parseIPv6(ip);
    public static Dictionary<string, string> ParseIpv6(string ip) => parseIPv6(ip);

    /// <summary>
    /// Parses an IPv6 address with a separate prefix length (e.g. parseIPv6("2001:db8::1", 64)).
    /// </summary>
    public static Dictionary<string, string> parseIPv6(string ip, int prefixLength) => ParseIPv6Info(ip, prefixLength).ToDictionary();

    /// <summary>
    /// PascalCase alias for <see cref="parseIPv6(string, int)"/>.
    /// </summary>
    public static Dictionary<string, string> ParseIPv6(string ip, int prefixLength) => parseIPv6(ip, prefixLength);

    /// <summary>
    /// Case-insensitive aliases for <see cref="parseIPv6(string, int)"/>.
    /// </summary>
    public static Dictionary<string, string> parseIpv6(string ip, int prefixLength) => parseIPv6(ip, prefixLength);
    public static Dictionary<string, string> ParseIpv6(string ip, int prefixLength) => parseIPv6(ip, prefixLength);

    /// <summary>
    /// Parses an IPv6 address and returns a strongly-typed <see cref="IPv6Info"/> object.
    /// </summary>
    public static IPv6Info ParseIPv6Info(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            throw new ArgumentException("IPv6 address cannot be null, empty, or whitespace.", nameof(ip));
        }

        string trimmed = ip.Trim();
        string ipPart;
        string? prefixPart = null;

        // Handle bracketed IPv6 addresses, e.g. [2001:db8::1]/64 or [2001:db8::1]
        if (trimmed.StartsWith('['))
        {
            int closeBracket = trimmed.IndexOf(']');
            if (closeBracket == -1)
            {
                throw new FormatException($"Malformed bracketed IPv6 address '{ip}'. Missing closing ']'.");
            }

            ipPart = trimmed.Substring(1, closeBracket - 1);
            string remainder = trimmed.Substring(closeBracket + 1).Trim();
            if (remainder.StartsWith('/'))
            {
                prefixPart = remainder.Substring(1).Trim();
            }
            else if (!string.IsNullOrEmpty(remainder))
            {
                prefixPart = remainder;
            }
        }
        else if (trimmed.Contains('/'))
        {
            var parts = trimmed.Split('/', StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[1]))
            {
                throw new FormatException($"Invalid IPv6 CIDR format '{ip}'. Expected format: 'IPv6/Prefix' (e.g. '2001:db8::1/64').");
            }
            ipPart = parts[0];
            prefixPart = parts[1];
        }
        else if (trimmed.Contains(' '))
        {
            var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                throw new FormatException($"Invalid IPv6 format '{ip}'. Expected 'IPv6 PrefixLength'.");
            }
            ipPart = parts[0];
            prefixPart = parts[1];
        }
        else
        {
            ipPart = trimmed;
            prefixPart = null;
        }

        int? prefixLength = null;
        if (!string.IsNullOrWhiteSpace(prefixPart))
        {
            string cleanPrefix = prefixPart.Trim().TrimStart('/');
            if (!int.TryParse(cleanPrefix, NumberStyles.Integer, CultureInfo.InvariantCulture, out int p))
            {
                throw new FormatException($"Invalid IPv6 prefix length '{prefixPart}'. Must be an integer between 0 and 128.");
            }
            if (p < 0 || p > 128)
            {
                throw new ArgumentOutOfRangeException(nameof(ip), $"IPv6 prefix length /{p} is invalid. Must be between 0 and 128.");
            }
            prefixLength = p;
        }

        return ParseIPv6Info(ipPart, prefixLength);
    }

    /// <summary>
    /// Parses an IPv6 address string and integer prefix length into a strongly-typed <see cref="IPv6Info"/> object.
    /// </summary>
    public static IPv6Info ParseIPv6Info(string ipPart, int? prefixLength)
    {
        if (string.IsNullOrWhiteSpace(ipPart))
        {
            throw new ArgumentException("IPv6 address cannot be null, empty, or whitespace.", nameof(ipPart));
        }

        string cleanIp = ipPart.Trim().TrimStart('[').TrimEnd(']');
        if (!IPAddress.TryParse(cleanIp, out var ipAddr) || ipAddr.AddressFamily != AddressFamily.InterNetworkV6)
        {
            throw new FormatException($"Invalid IPv6 address format: '{ipPart}'.");
        }

        byte[] ipBytes = ipAddr.GetAddressBytes();
        int prefix = prefixLength ?? (IPAddress.IsLoopback(ipAddr) ? 128 : 64);

        if (prefix < 0 || prefix > 128)
        {
            throw new ArgumentOutOfRangeException(nameof(prefixLength), $"IPv6 prefix length /{prefix} is invalid. Must be between 0 and 128.");
        }

        BigInteger ipBigInt = new BigInteger(ipBytes, isUnsigned: true, isBigEndian: true);
        BigInteger max128 = (BigInteger.One << 128) - 1;

        BigInteger mask = prefix == 0 ? BigInteger.Zero : (max128 << (128 - prefix)) & max128;
        BigInteger wildcard = ~mask & max128;

        BigInteger networkBigInt = ipBigInt & mask;
        BigInteger lastBigInt = networkBigInt | wildcard;

        string canonicalIp = ipAddr.ToString();
        string expandedIp = FormatExpandedIPv6(ipBytes);

        byte[] netBytes = BigIntTo16Bytes(networkBigInt);
        string canonicalNetwork = new IPAddress(netBytes).ToString();
        string expandedNetwork = FormatExpandedIPv6(netBytes);

        byte[] maskBytes = BigIntTo16Bytes(mask);
        string prefixNetmask = FormatExpandedIPv6(maskBytes);

        BigInteger totalHosts = BigInteger.One << (128 - prefix);

        string firstUsableIp;
        string lastUsableIp;
        string usableRange;
        BigInteger usableHosts;

        if (prefix == 128)
        {
            firstUsableIp = canonicalIp;
            lastUsableIp = canonicalIp;
            usableRange = canonicalIp;
            usableHosts = BigInteger.One;
        }
        else if (prefix == 127)
        {
            firstUsableIp = canonicalNetwork;
            byte[] lastBytes = BigIntTo16Bytes(lastBigInt);
            lastUsableIp = new IPAddress(lastBytes).ToString();
            usableRange = $"{firstUsableIp} - {lastUsableIp}";
            usableHosts = 2;
        }
        else
        {
            byte[] firstBytes = BigIntTo16Bytes(networkBigInt + 1);
            firstUsableIp = new IPAddress(firstBytes).ToString();

            byte[] lastBytes = BigIntTo16Bytes(lastBigInt);
            lastUsableIp = new IPAddress(lastBytes).ToString();

            usableRange = $"{firstUsableIp} - {lastUsableIp}";
            usableHosts = totalHosts > 1 ? totalHosts - 1 : totalHosts;
        }

        string ipType = GetIPv6Type(ipBytes, ipAddr);
        string reverseDns = GetReverseDnsPointer(ipBytes);
        string binaryIp = ToBinary128(ipBytes);
        string hexIp = "0x" + Convert.ToHexString(ipBytes);

        return new IPv6Info
        {
            IpAddress = canonicalIp,
            ExpandedIpAddress = expandedIp,
            NetworkAddress = canonicalNetwork,
            ExpandedNetworkAddress = expandedNetwork,
            PrefixLength = prefix,
            PrefixNetmask = prefixNetmask,
            FirstUsableIp = firstUsableIp,
            LastUsableIp = lastUsableIp,
            UsableHostRange = usableRange,
            TotalHosts = totalHosts,
            UsableHosts = usableHosts,
            IpType = ipType,
            ReverseDnsPointer = reverseDns,
            BinaryIp = binaryIp,
            HexIp = hexIp
        };
    }

    // ==========================================
    // Internal Helper Methods
    // ==========================================

    private static byte[] ParseOctets(string input, string fieldName)
    {
        var parts = input.Split('.');
        if (parts.Length != 4)
        {
            throw new FormatException($"Invalid {fieldName} '{input}'. It must consist of exactly 4 octets separated by dots.");
        }

        byte[] octets = new byte[4];
        for (int i = 0; i < 4; i++)
        {
            if (!byte.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out octets[i]))
            {
                throw new FormatException($"Invalid octet '{parts[i]}' in {fieldName} '{input}'. Each octet must be an integer between 0 and 255.");
            }
        }
        return octets;
    }

    private static uint OctetsToUint(byte[] octets)
    {
        return ((uint)octets[0] << 24) | ((uint)octets[1] << 16) | ((uint)octets[2] << 8) | (uint)octets[3];
    }

    private static string UintToIp(uint value)
    {
        return $"{(value >> 24) & 0xFF}.{(value >> 16) & 0xFF}.{(value >> 8) & 0xFF}.{value & 0xFF}";
    }

    private static string ToBinaryDotted(uint value)
    {
        return $"{Convert.ToString((value >> 24) & 0xFF, 2).PadLeft(8, '0')}." +
               $"{Convert.ToString((value >> 16) & 0xFF, 2).PadLeft(8, '0')}." +
               $"{Convert.ToString((value >> 8) & 0xFF, 2).PadLeft(8, '0')}." +
               $"{Convert.ToString(value & 0xFF, 2).PadLeft(8, '0')}";
    }

    private static int GetDefaultCidr(byte firstOctet) => firstOctet switch
    {
        <= 127 => 8,
        <= 191 => 16,
        <= 223 => 24,
        _ => 32
    };

    private static string GetIpClass(byte firstOctet) => firstOctet switch
    {
        <= 127 => "Class A",
        <= 191 => "Class B",
        <= 223 => "Class C",
        <= 239 => "Class D (Multicast)",
        _ => "Class E (Experimental/Reserved)"
    };

    private static string GetIpType(byte[] octets)
    {
        if (octets[0] == 10)
            return "Private (RFC 1918)";
        if (octets[0] == 172 && octets[1] >= 16 && octets[1] <= 31)
            return "Private (RFC 1918)";
        if (octets[0] == 192 && octets[1] == 168)
            return "Private (RFC 1918)";
        if (octets[0] == 127)
            return "Loopback (RFC 1122)";
        if (octets[0] == 169 && octets[1] == 254)
            return "Link-Local / APIPA (RFC 3927)";
        if (octets[0] == 100 && octets[1] >= 64 && octets[1] <= 127)
            return "Carrier-Grade NAT / CGNAT (RFC 6598)";
        if (octets[0] == 0)
            return "Current Network / This Host (RFC 1122)";
        if (octets[0] >= 224 && octets[0] <= 239)
            return "Multicast (RFC 5771)";
        if (octets[0] == 255 && octets[1] == 255 && octets[2] == 255 && octets[3] == 255)
            return "Limited Broadcast (RFC 919)";
        if (octets[0] >= 240)
            return "Reserved for Future Use (RFC 1112)";

        return "Public";
    }

    private static string FormatExpandedIPv6(byte[] bytes)
    {
        return $"{bytes[0]:x2}{bytes[1]:x2}:{bytes[2]:x2}{bytes[3]:x2}:{bytes[4]:x2}{bytes[5]:x2}:{bytes[6]:x2}{bytes[7]:x2}:" +
               $"{bytes[8]:x2}{bytes[9]:x2}:{bytes[10]:x2}{bytes[11]:x2}:{bytes[12]:x2}{bytes[13]:x2}:{bytes[14]:x2}{bytes[15]:x2}";
    }

    private static byte[] BigIntTo16Bytes(BigInteger value)
    {
        byte[] minimal = value.ToByteArray(isUnsigned: true, isBigEndian: true);
        byte[] bytes = new byte[16];
        if (minimal.Length > 16)
        {
            Array.Copy(minimal, minimal.Length - 16, bytes, 0, 16);
        }
        else
        {
            Array.Copy(minimal, 0, bytes, 16 - minimal.Length, minimal.Length);
        }
        return bytes;
    }

    private static string GetIPv6Type(byte[] b, IPAddress ipAddr)
    {
        if (IPAddress.IsLoopback(ipAddr))
            return "Loopback (RFC 4291)";
        if (ipAddr.IsIPv6LinkLocal)
            return "Link-Local Unicast (RFC 4291)";
        if (ipAddr.IsIPv6Multicast)
            return "Multicast (RFC 4291)";
        if (ipAddr.IsIPv6SiteLocal)
            return "Site-Local Unicast (Deprecated RFC 3879)";
        if (ipAddr.IsIPv4MappedToIPv6)
            return "IPv4-Mapped IPv6 (RFC 4291)";

        bool allZeros = true;
        for (int i = 0; i < 16; i++)
        {
            if (b[i] != 0) { allZeros = false; break; }
        }
        if (allZeros) return "Unspecified (RFC 4291)";

        // ULA fc00::/7
        if ((b[0] & 0xFE) == 0xFC)
            return "Unique Local Unicast / ULA (RFC 4193)";

        // Documentation 2001:db8::/32
        if (b[0] == 0x20 && b[1] == 0x01 && b[2] == 0x0D && b[3] == 0xB8)
            return "Documentation (RFC 3849)";

        // Teredo 2001:0000::/32
        if (b[0] == 0x20 && b[1] == 0x01 && b[2] == 0x00 && b[3] == 0x00)
            return "Teredo Tunneling (RFC 4380)";

        // 6to4 2002::/16
        if (b[0] == 0x20 && b[1] == 0x02)
            return "6to4 Tunneling (RFC 3056)";

        // NAT64 64:ff9b::/96
        if (b[0] == 0x00 && b[1] == 0x64 && b[2] == 0xFF && b[3] == 0x9B)
            return "IPv4/IPv6 Translation / NAT64 (RFC 6052)";

        // Discard-Only 100::/64
        if (b[0] == 0x01 && b[1] == 0x00)
            return "Discard-Only Prefix (RFC 6666)";

        // Global Unicast 2000::/3
        if ((b[0] & 0xE0) == 0x20)
            return "Global Unicast / GUA (RFC 3587)";

        return "Reserved";
    }

    private static string ToBinary128(byte[] b)
    {
        var parts = new string[8];
        for (int i = 0; i < 8; i++)
        {
            int val = (b[i * 2] << 8) | b[i * 2 + 1];
            parts[i] = Convert.ToString(val, 2).PadLeft(16, '0');
        }
        return string.Join(":", parts);
    }

    private static string GetReverseDnsPointer(byte[] b)
    {
        var hex = Convert.ToHexString(b).ToLowerInvariant();
        var sb = new StringBuilder();
        for (int i = hex.Length - 1; i >= 0; i--)
        {
            sb.Append(hex[i]).Append('.');
        }
        sb.Append("ip6.arpa");
        return sb.ToString();
    }
}

/// <summary>
/// Alias for <see cref="IPCalci"/> matching original template class name Class1.
/// </summary>
public class Class1 : IPCalci
{
}

/// <summary>
/// Alias for <see cref="IPCalci"/> as IPCalculator.
/// </summary>
public class IPCalculator : IPCalci
{
}

/// <summary>
/// Extension methods for <see cref="IPCalci"/> allowing instance-style method calls.
/// </summary>
public static class IPCalciExtensions
{
    // Generic auto-detect parse
    public static Dictionary<string, string> parse(this IPCalci calci, string ip) => IPCalci.parse(ip);
    public static Dictionary<string, string> Parse(this IPCalci calci, string ip) => IPCalci.Parse(ip);
    public static Dictionary<string, string> parse(this IPCalci calci, string ip, string subnetOrPrefix) => IPCalci.parse(ip, subnetOrPrefix);
    public static Dictionary<string, string> Parse(this IPCalci calci, string ip, string subnetOrPrefix) => IPCalci.Parse(ip, subnetOrPrefix);

    // IPv4 instance methods
    public static Dictionary<string, string> parseIPv4(this IPCalci calci, string ip) => IPCalci.parseIPv4(ip);
    public static Dictionary<string, string> ParseIPv4(this IPCalci calci, string ip) => IPCalci.ParseIPv4(ip);
    public static Dictionary<string, string> parseIpv4(this IPCalci calci, string ip) => IPCalci.parseIPv4(ip);
    public static Dictionary<string, string> ParseIpv4(this IPCalci calci, string ip) => IPCalci.ParseIPv4(ip);

    public static Dictionary<string, string> parseIPv4(this IPCalci calci, string ip, string subnetMaskOrCidr)
        => IPCalci.parseIPv4(ip, subnetMaskOrCidr);
    public static Dictionary<string, string> ParseIPv4(this IPCalci calci, string ip, string subnetMaskOrCidr)
        => IPCalci.ParseIPv4(ip, subnetMaskOrCidr);
    public static Dictionary<string, string> parseIpv4(this IPCalci calci, string ip, string subnetMaskOrCidr)
        => IPCalci.parseIPv4(ip, subnetMaskOrCidr);
    public static Dictionary<string, string> ParseIpv4(this IPCalci calci, string ip, string subnetMaskOrCidr)
        => IPCalci.ParseIPv4(ip, subnetMaskOrCidr);

    // IPv6 instance methods
    public static Dictionary<string, string> parseIPv6(this IPCalci calci, string ip) => IPCalci.parseIPv6(ip);
    public static Dictionary<string, string> ParseIPv6(this IPCalci calci, string ip) => IPCalci.ParseIPv6(ip);
    public static Dictionary<string, string> parseIpv6(this IPCalci calci, string ip) => IPCalci.parseIPv6(ip);
    public static Dictionary<string, string> ParseIpv6(this IPCalci calci, string ip) => IPCalci.ParseIPv6(ip);

    public static Dictionary<string, string> parseIPv6(this IPCalci calci, string ip, int prefixLength)
        => IPCalci.parseIPv6(ip, prefixLength);
    public static Dictionary<string, string> ParseIPv6(this IPCalci calci, string ip, int prefixLength)
        => IPCalci.ParseIPv6(ip, prefixLength);
    public static Dictionary<string, string> parseIpv6(this IPCalci calci, string ip, int prefixLength)
        => IPCalci.parseIPv6(ip, prefixLength);
    public static Dictionary<string, string> ParseIpv6(this IPCalci calci, string ip, int prefixLength)
        => IPCalci.ParseIPv6(ip, prefixLength);

    public static List<string> ParseToList(this IPCalci calci, string ip, bool withLabels = true) => IPCalci.ParseToList(ip, withLabels);
    public static IPv4Info ParseInfo(this IPCalci calci, string ip) => IPCalci.ParseInfo(ip);
    public static IPv4Info ParseIPv4Info(this IPCalci calci, string ip) => IPCalci.ParseIPv4Info(ip);
    public static IPv6Info ParseIPv6Info(this IPCalci calci, string ip) => IPCalci.ParseIPv6Info(ip);
}

/// <summary>
/// Represents comprehensive IPv4 network and addressing details.
/// </summary>
public class IPv4Info
{
    public string IpAddress { get; set; } = string.Empty;
    public string NetworkAddress { get; set; } = string.Empty;
    public string SubnetMask { get; set; } = string.Empty;
    public int Cidr { get; set; }
    public string CidrNotation => $"/{Cidr}";
    public string FirstUsableIp { get; set; } = string.Empty;
    public string LastUsableIp { get; set; } = string.Empty;
    public string BroadcastAddress { get; set; } = string.Empty;
    public string UsableHostRange { get; set; } = string.Empty;
    public long TotalHosts { get; set; }
    public long UsableHosts { get; set; }
    public string WildcardMask { get; set; } = string.Empty;
    public string IpClass { get; set; } = string.Empty;
    public string IpType { get; set; } = string.Empty;
    public string BinaryIp { get; set; } = string.Empty;
    public string BinarySubnetMask { get; set; } = string.Empty;
    public string HexIp { get; set; } = string.Empty;
    public uint DecimalIp { get; set; }
    public string CidrNetwork => $"{NetworkAddress}/{Cidr}";

    public Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Network", NetworkAddress },
            { "Subnet Mask", SubnetMask },
            { "CIDR Notation", CidrNotation },
            { "First Usable IP", FirstUsableIp },
            { "Broadcast Address", BroadcastAddress },
            { "Last Usable IP", LastUsableIp },
            { "Usable Host Range", UsableHostRange },
            { "Total Number of Hosts", TotalHosts.ToString(CultureInfo.InvariantCulture) },
            { "Number of Usable Hosts", UsableHosts.ToString(CultureInfo.InvariantCulture) },
            { "Wildcard Mask", WildcardMask },
            { "IP Class", IpClass },
            { "IP Type", IpType },
            { "IP Address", IpAddress },
            { "CIDR Network", CidrNetwork },
            { "Binary IP Address", BinaryIp },
            { "Binary Subnet Mask", BinarySubnetMask },
            { "Hex IP Address", HexIp },
            { "Decimal IP Address", DecimalIp.ToString(CultureInfo.InvariantCulture) },

            { "Network Address", NetworkAddress },
            { "SubnetMask", SubnetMask },
            { "CIDR", CidrNotation },
            { "FirstUsableIp", FirstUsableIp },
            { "BroadcastAddress", BroadcastAddress },
            { "Broadcast", BroadcastAddress },
            { "LastUsableIp", LastUsableIp },
            { "UsableHostRange", UsableHostRange },
            { "Total Hosts", TotalHosts.ToString(CultureInfo.InvariantCulture) },
            { "TotalHosts", TotalHosts.ToString(CultureInfo.InvariantCulture) },
            { "Usable Hosts", UsableHosts.ToString(CultureInfo.InvariantCulture) },
            { "UsableHosts", UsableHosts.ToString(CultureInfo.InvariantCulture) },
            { "WildcardMask", WildcardMask },
            { "Wildcard", WildcardMask },
            { "IpClass", IpClass },
            { "Class", IpClass },
            { "IpType", IpType },
            { "Type", IpType },
            { "IpAddress", IpAddress },
            { "IP", IpAddress },
            { "CidrNetwork", CidrNetwork },
            { "Binary IP", BinaryIp },
            { "BinaryIp", BinaryIp },
            { "BinarySubnetMask", BinarySubnetMask },
            { "Binary Mask", BinarySubnetMask },
            { "Hex IP", HexIp },
            { "HexIp", HexIp },
            { "Decimal IP", DecimalIp.ToString(CultureInfo.InvariantCulture) },
            { "DecimalIp", DecimalIp.ToString(CultureInfo.InvariantCulture) }
        };
    }

    public List<string> ToList(bool withLabels = true)
    {
        if (withLabels)
        {
            return new List<string>
            {
                $"Network: {NetworkAddress}",
                $"Subnet Mask: {SubnetMask}",
                $"CIDR Notation: {CidrNotation}",
                $"First Usable IP: {FirstUsableIp}",
                $"Broadcast Address: {BroadcastAddress}",
                $"Last Usable IP: {LastUsableIp}",
                $"Usable Host Range: {UsableHostRange}",
                $"Total Number of Hosts: {TotalHosts}",
                $"Number of Usable Hosts: {UsableHosts}",
                $"Wildcard Mask: {WildcardMask}",
                $"IP Class: {IpClass}",
                $"IP Type: {IpType}",
                $"IP Address: {IpAddress}",
                $"CIDR Network: {CidrNetwork}",
                $"Binary IP Address: {BinaryIp}",
                $"Binary Subnet Mask: {BinarySubnetMask}",
                $"Hex IP Address: {HexIp}",
                $"Decimal IP Address: {DecimalIp}"
            };
        }

        return new List<string>
        {
            NetworkAddress,
            SubnetMask,
            CidrNotation,
            FirstUsableIp,
            BroadcastAddress,
            LastUsableIp,
            UsableHostRange,
            TotalHosts.ToString(CultureInfo.InvariantCulture),
            UsableHosts.ToString(CultureInfo.InvariantCulture),
            WildcardMask,
            IpClass,
            IpType,
            IpAddress,
            CidrNetwork,
            BinaryIp,
            BinarySubnetMask,
            HexIp,
            DecimalIp.ToString(CultureInfo.InvariantCulture)
        };
    }
}

/// <summary>
/// Represents comprehensive IPv6 network and addressing details.
/// </summary>
public class IPv6Info
{
    public string IpAddress { get; set; } = string.Empty;
    public string ExpandedIpAddress { get; set; } = string.Empty;
    public string NetworkAddress { get; set; } = string.Empty;
    public string ExpandedNetworkAddress { get; set; } = string.Empty;
    public int PrefixLength { get; set; }
    public string CidrNotation => $"/{PrefixLength}";
    public string PrefixNetmask { get; set; } = string.Empty;
    public string CidrNetwork => $"{NetworkAddress}/{PrefixLength}";
    public string FirstUsableIp { get; set; } = string.Empty;
    public string LastUsableIp { get; set; } = string.Empty;
    public string UsableHostRange { get; set; } = string.Empty;
    public BigInteger TotalHosts { get; set; }
    public BigInteger UsableHosts { get; set; }
    public string IpType { get; set; } = string.Empty;
    public string ReverseDnsPointer { get; set; } = string.Empty;
    public string BinaryIp { get; set; } = string.Empty;
    public string HexIp { get; set; } = string.Empty;

    public Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "IP Address", IpAddress },
            { "Expanded IP Address", ExpandedIpAddress },
            { "Network", NetworkAddress },
            { "Expanded Network", ExpandedNetworkAddress },
            { "CIDR Notation", CidrNotation },
            { "Prefix Length", PrefixLength.ToString(CultureInfo.InvariantCulture) },
            { "Prefix Netmask", PrefixNetmask },
            { "CIDR Network", CidrNetwork },
            { "First Usable IP", FirstUsableIp },
            { "Last Usable IP", LastUsableIp },
            { "Usable Host Range", UsableHostRange },
            { "Total Number of Hosts", TotalHosts.ToString(CultureInfo.InvariantCulture) },
            { "Number of Usable Hosts", UsableHosts.ToString(CultureInfo.InvariantCulture) },
            { "IP Type", IpType },
            { "Reverse DNS Pointer", ReverseDnsPointer },
            { "Binary IP Address", BinaryIp },
            { "Hex IP Address", HexIp },

            // Convenient aliases:
            { "Network Address", NetworkAddress },
            { "Subnet Mask", PrefixNetmask },
            { "SubnetMask", PrefixNetmask },
            { "CIDR", CidrNotation },
            { "FirstUsableIp", FirstUsableIp },
            { "LastUsableIp", LastUsableIp },
            { "UsableHostRange", UsableHostRange },
            { "Total Hosts", TotalHosts.ToString(CultureInfo.InvariantCulture) },
            { "TotalHosts", TotalHosts.ToString(CultureInfo.InvariantCulture) },
            { "Usable Hosts", UsableHosts.ToString(CultureInfo.InvariantCulture) },
            { "UsableHosts", UsableHosts.ToString(CultureInfo.InvariantCulture) },
            { "IpType", IpType },
            { "Type", IpType },
            { "IpAddress", IpAddress },
            { "IP", IpAddress },
            { "CidrNetwork", CidrNetwork },
            { "ReverseDnsPointer", ReverseDnsPointer },
            { "Reverse DNS", ReverseDnsPointer },
            { "Binary IP", BinaryIp },
            { "BinaryIp", BinaryIp },
            { "Hex IP", HexIp },
            { "HexIp", HexIp }
        };
    }

    public List<string> ToList(bool withLabels = true)
    {
        if (withLabels)
        {
            return new List<string>
            {
                $"IP Address: {IpAddress}",
                $"Expanded IP: {ExpandedIpAddress}",
                $"Network: {NetworkAddress}",
                $"Expanded Network: {ExpandedNetworkAddress}",
                $"CIDR Notation: {CidrNotation}",
                $"Prefix Netmask: {PrefixNetmask}",
                $"CIDR Network: {CidrNetwork}",
                $"First Usable IP: {FirstUsableIp}",
                $"Last Usable IP: {LastUsableIp}",
                $"Usable Host Range: {UsableHostRange}",
                $"Total Number of Hosts: {TotalHosts}",
                $"Number of Usable Hosts: {UsableHosts}",
                $"IP Type: {IpType}",
                $"Reverse DNS: {ReverseDnsPointer}",
                $"Binary IP: {BinaryIp}",
                $"Hex IP: {HexIp}"
            };
        }

        return new List<string>
        {
            IpAddress,
            ExpandedIpAddress,
            NetworkAddress,
            ExpandedNetworkAddress,
            CidrNotation,
            PrefixNetmask,
            CidrNetwork,
            FirstUsableIp,
            LastUsableIp,
            UsableHostRange,
            TotalHosts.ToString(CultureInfo.InvariantCulture),
            UsableHosts.ToString(CultureInfo.InvariantCulture),
            IpType,
            ReverseDnsPointer,
            BinaryIp,
            HexIp
        };
    }
}
