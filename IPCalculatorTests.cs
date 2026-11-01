using System;
using System.Collections.Generic;
using System.Numerics;
using Xunit;

namespace IPCalci;

public class IPCalculatorTests
{
    // ==========================================
    // IPv4 Tests
    // ==========================================

    [Fact]
    public void ParseIPv4_StandardClassC24_ReturnsCorrectCalculations()
    {
        var result = IPCalci.parseIPv4("192.168.1.55/24");

        Assert.Equal("192.168.1.0", result["Network"]);
        Assert.Equal("255.255.255.0", result["Subnet Mask"]);
        Assert.Equal("/24", result["CIDR Notation"]);
        Assert.Equal("192.168.1.1", result["First Usable IP"]);
        Assert.Equal("192.168.1.255", result["Broadcast Address"]);
        Assert.Equal("192.168.1.254", result["Last Usable IP"]);
        Assert.Equal("192.168.1.1 - 192.168.1.254", result["Usable Host Range"]);
        Assert.Equal("256", result["Total Number of Hosts"]);
        Assert.Equal("254", result["Number of Usable Hosts"]);
        Assert.Equal("0.0.0.255", result["Wildcard Mask"]);
        Assert.Equal("Class C", result["IP Class"]);
        Assert.Equal("Private (RFC 1918)", result["IP Type"]);
        Assert.Equal("192.168.1.55", result["IP Address"]);
        Assert.Equal("192.168.1.0/24", result["CIDR Network"]);
    }

    [Theory]
    [InlineData("network")]
    [InlineData("NETWORK")]
    [InlineData("Network")]
    [InlineData("Network Address")]
    [InlineData("subnet mask")]
    [InlineData("SubnetMask")]
    [InlineData("cidr notation")]
    [InlineData("CIDR")]
    [InlineData("first usable ip")]
    [InlineData("FirstUsableIp")]
    [InlineData("broadcast address")]
    [InlineData("Broadcast")]
    public void ParseIPv4_KeyLookup_IsCaseInsensitiveAndSupportsAliases(string key)
    {
        var result = IPCalci.parseIPv4("192.168.1.1/24");

        Assert.True(result.ContainsKey(key));
        Assert.False(string.IsNullOrEmpty(result[key]));
    }

    [Fact]
    public void ParseIPv4_InstanceMethodCall_Succeeds()
    {
        var calci = new IPCalci();
        var result = calci.parseIPv4("10.20.30.40/8");

        Assert.Equal("10.0.0.0", result["Network"]);
        Assert.Equal("255.0.0.0", result["Subnet Mask"]);
        Assert.Equal("/8", result["CIDR Notation"]);
        Assert.Equal("10.0.0.1", result["First Usable IP"]);
        Assert.Equal("10.255.255.255", result["Broadcast Address"]);
        Assert.Equal("Class A", result["IP Class"]);
        Assert.Equal("Private (RFC 1918)", result["IP Type"]);
    }

    [Fact]
    public void ParseIPv4_SeparateIpAndMaskArguments_Succeeds()
    {
        var result = IPCalci.parseIPv4("172.16.50.1", "255.255.0.0");

        Assert.Equal("172.16.0.0", result["Network"]);
        Assert.Equal("255.255.0.0", result["Subnet Mask"]);
        Assert.Equal("/16", result["CIDR Notation"]);
        Assert.Equal("172.16.255.255", result["Broadcast Address"]);
        Assert.Equal("Class B", result["IP Class"]);
    }

    [Theory]
    [InlineData("10.5.5.5", "/8", "Class A")]
    [InlineData("172.20.1.1", "/16", "Class B")]
    [InlineData("192.168.1.100", "/24", "Class C")]
    public void ParseIPv4_WithoutSubnetSpecified_InfersClassfulDefaultMask(string ip, string expectedCidr, string expectedClass)
    {
        var result = IPCalci.parseIPv4(ip);

        Assert.Equal(expectedCidr, result["CIDR Notation"]);
        Assert.Equal(expectedClass, result["IP Class"]);
    }

    [Fact]
    public void ParseIPv4_PointToPointSubnet31_CalculatesRfc3021Correctly()
    {
        var result = IPCalci.parseIPv4("192.168.1.2/31");

        Assert.Equal("192.168.1.2", result["Network"]);
        Assert.Equal("192.168.1.3", result["Broadcast Address"]);
        Assert.Equal("192.168.1.2", result["First Usable IP"]);
        Assert.Equal("192.168.1.3", result["Last Usable IP"]);
        Assert.Equal("2", result["Usable Hosts"]);
    }

    [Fact]
    public void ParseIPv4_SingleHostRoute32_CalculatesCorrectly()
    {
        var result = IPCalci.parseIPv4("8.8.8.8/32");

        Assert.Equal("8.8.8.8", result["Network"]);
        Assert.Equal("8.8.8.8", result["First Usable IP"]);
        Assert.Equal("8.8.8.8", result["Last Usable IP"]);
        Assert.Equal("1", result["Total Hosts"]);
        Assert.Equal("1", result["Usable Hosts"]);
        Assert.Equal("Public", result["IP Type"]);
    }

    [Theory]
    [InlineData("127.0.0.1/8", "Loopback (RFC 1122)")]
    [InlineData("169.254.10.20/16", "Link-Local / APIPA (RFC 3927)")]
    [InlineData("100.64.1.1/10", "Carrier-Grade NAT / CGNAT (RFC 6598)")]
    [InlineData("8.8.4.4/32", "Public")]
    public void ParseIPv4_IpType_IdentifiesCorrectScope(string ip, string expectedScope)
    {
        var result = IPCalci.parseIPv4(ip);
        Assert.Equal(expectedScope, result["IP Type"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseIPv4_EmptyOrWhitespace_ThrowsArgumentException(string invalidIp)
    {
        Assert.Throws<ArgumentException>(() => IPCalci.parseIPv4(invalidIp));
    }

    [Theory]
    [InlineData("300.1.1.1/24")]
    [InlineData("192.168.1/24")]
    [InlineData("192.168.1.1.1/24")]
    [InlineData("abc.def.ghi.jkl/24")]
    public void ParseIPv4_MalformedOctets_ThrowsFormatException(string malformedIp)
    {
        Assert.Throws<FormatException>(() => IPCalci.parseIPv4(malformedIp));
    }

    [Theory]
    [InlineData("192.168.1.1/33")]
    [InlineData("192.168.1.1/-1")]
    public void ParseIPv4_CidrOutOfRange_ThrowsArgumentOutOfRangeException(string invalidCidrIp)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => IPCalci.parseIPv4(invalidCidrIp));
    }

    [Fact]
    public void ParseIPv4_NonContiguousSubnetMask_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => IPCalci.parseIPv4("192.168.1.1", "255.255.0.255"));
    }

    // ==========================================
    // IPv6 Tests
    // ==========================================

    [Fact]
    public void ParseIPv6_Standard64_ReturnsCorrectCalculations()
    {
        var result = IPCalci.parseIPv6("2001:db8:85a3::8a2e:370:7334/64");

        Assert.Equal("2001:db8:85a3::8a2e:370:7334", result["IP Address"]);
        Assert.Equal("2001:0db8:85a3:0000:0000:8a2e:0370:7334", result["Expanded IP Address"]);
        Assert.Equal("2001:db8:85a3::", result["Network"]);
        Assert.Equal("2001:0db8:85a3:0000:0000:0000:0000:0000", result["Expanded Network"]);
        Assert.Equal("/64", result["CIDR Notation"]);
        Assert.Equal("64", result["Prefix Length"]);
        Assert.Equal("ffff:ffff:ffff:ffff:0000:0000:0000:0000", result["Prefix Netmask"]);
        Assert.Equal("2001:db8:85a3::/64", result["CIDR Network"]);
        Assert.Equal("2001:db8:85a3::1", result["First Usable IP"]);
        Assert.Equal("2001:db8:85a3:0:ffff:ffff:ffff:ffff", result["Last Usable IP"]);
        Assert.Equal("18446744073709551616", result["Total Number of Hosts"]);
        Assert.Equal("Documentation (RFC 3849)", result["IP Type"]);
    }

    [Fact]
    public void ParseIPv6_BracketNotation_Succeeds()
    {
        var result = IPCalci.parseIPv6("[2001:db8::1]/64");

        Assert.Equal("2001:db8::1", result["IP Address"]);
        Assert.Equal("2001:db8::", result["Network"]);
        Assert.Equal("/64", result["CIDR Notation"]);
    }

    [Fact]
    public void ParseIPv6_SeparatePrefixArgument_Succeeds()
    {
        var result = IPCalci.parseIPv6("2001:db8::1", 48);

        Assert.Equal("2001:db8::", result["Network"]);
        Assert.Equal("/48", result["CIDR Notation"]);
        Assert.Equal("ffff:ffff:ffff:0000:0000:0000:0000:0000", result["Prefix Netmask"]);
    }

    [Theory]
    [InlineData("::1/128", "Loopback (RFC 4291)")]
    [InlineData("fe80::1/64", "Link-Local Unicast (RFC 4291)")]
    [InlineData("fd00::1/64", "Unique Local Unicast / ULA (RFC 4193)")]
    [InlineData("ff02::1/8", "Multicast (RFC 4291)")]
    [InlineData("2607:f8b0:4005:805::200e/64", "Global Unicast / GUA (RFC 3587)")]
    public void ParseIPv6_ScopeIdentification_DetectsCorrectType(string ipv6, string expectedScope)
    {
        var result = IPCalci.parseIPv6(ipv6);
        Assert.Equal(expectedScope, result["IP Type"]);
    }

    [Fact]
    public void ParseIPv6_Loopback128_CalculatesCorrectly()
    {
        var result = IPCalci.parseIPv6("::1/128");

        Assert.Equal("::1", result["IP Address"]);
        Assert.Equal("::1", result["Network"]);
        Assert.Equal("::1", result["First Usable IP"]);
        Assert.Equal("::1", result["Last Usable IP"]);
        Assert.Equal("1", result["Total Number of Hosts"]);
    }

    [Fact]
    public void ParseIPv6_PointToPoint127_CalculatesRfc6164Correctly()
    {
        var result = IPCalci.parseIPv6("2001:db8::/127");

        Assert.Equal("2001:db8::", result["Network"]);
        Assert.Equal("2001:db8::", result["First Usable IP"]);
        Assert.Equal("2001:db8::1", result["Last Usable IP"]);
        Assert.Equal("2", result["Total Number of Hosts"]);
    }

    [Fact]
    public void ParseIPv6_ReverseDnsPointer_FormatsCorrectly()
    {
        var result = IPCalci.parseIPv6("2001:db8::1/64");

        // Reverse nibble of 2001:0db8:0000:0000:0000:0000:0000:0001
        Assert.StartsWith("1.0.0.0.0.0.0.0", result["Reverse DNS Pointer"]);
        Assert.EndsWith("8.b.d.0.1.0.0.2.ip6.arpa", result["Reverse DNS Pointer"]);
    }

    [Theory]
    [InlineData("2001:db8::1/129")]
    [InlineData("2001:db8::1/-1")]
    public void ParseIPv6_PrefixOutOfRange_ThrowsArgumentOutOfRangeException(string invalidPrefix)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => IPCalci.parseIPv6(invalidPrefix));
    }

    [Theory]
    [InlineData("2001:::1/64")]
    [InlineData("2001:xyz::1/64")]
    [InlineData("invalid-ipv6")]
    public void ParseIPv6_MalformedAddress_ThrowsFormatException(string malformed)
    {
        Assert.Throws<FormatException>(() => IPCalci.parseIPv6(malformed));
    }

    // ==========================================
    // Unified Parse Method (Auto-detect version)
    // ==========================================

    [Fact]
    public void Parse_AutoDetects_IPv4AndIPv6()
    {
        var ipv4Result = IPCalci.parse("192.168.1.1/24");
        Assert.Equal("192.168.1.0", ipv4Result["Network"]);
        Assert.Equal("Class C", ipv4Result["IP Class"]);

        var ipv6Result = IPCalci.parse("2001:db8::1/64");
        Assert.Equal("2001:db8::", ipv6Result["Network"]);
        Assert.Equal("Documentation (RFC 3849)", ipv6Result["IP Type"]);
    }

    [Fact]
    public void ParseInfo_ReturnsStronglyTypedObjects()
    {
        var ipv4 = IPCalci.ParseIPv4Info("192.168.1.1/24");
        Assert.Equal("192.168.1.0", ipv4.NetworkAddress);
        Assert.Equal(24, ipv4.Cidr);

        var ipv6 = IPCalci.ParseIPv6Info("2001:db8::1/64");
        Assert.Equal("2001:db8::", ipv6.NetworkAddress);
        Assert.Equal(64, ipv6.PrefixLength);
        Assert.Equal(BigInteger.One << 64, ipv6.TotalHosts);
    }
}
