---
title: Notes on Kerberos and Active Directory
layout: post
date: 2015-11-02
category: archived
---

These are just some notes I made while reading about Kerberos and Active Directory. They aren't very technical or well-edited and probably have plenty of errors. Read at your peril!

## Kerberos

- <https://en.wikipedia.org/wiki/Kerberos_(protocol)>
- Requires a trusted third party
	- Authentication Server and Ticket Granting Service
	- usually backed by a database - Active Directory for example
- Originally implemented by MIT
- Windows 2000 onwards uses the Kerberos protocol (_not_ MIT's implementation) as the default auth method
	- Prior to Windows 2000 it used NTLM
	- If the machine is not joined to a domain it falls back to NTML between client and server
	- Intranet web applications can enforce Kerberos as an authentication method for domain joined clients by using APIs provided under SSPI
		- IIS integrated auth for example

![Kerberos Data Flow](/images/2015-11-02-notes-on-kerberos-and-active-directory/kerberos-data-flow.jpg)


### Authentication and SSO service access

#### Stage 1 - Initial sign-on

1. Client sends user ID (in plain text) to authentication server (AS)
1.  AS finds the user in the database (Active Directory) and sends:
	- TGS Session Key - encrypted with user's password (from AD)
	- Ticket Granting Ticket (TGT) - encrypted with the AS's private key
1. Client decrypts Session Key using the password the user entered
	- If it can't decrypt the Session Key then it can't be used and authentication has failed

Now the client has enough information to access the Ticket Granting Server.

#### Stage 2 - Getting Client/Server ticket, so the client can access a Service Server (SS)

1. Client sends to Ticket Granting Server (TGS):
	- TGT and ID of requested service
	- Authenticator, encrypted using the TGS Session Key
1. TGS decrypts the TGT using the AS's private key, which gives it the session key.
1. TGS uses the session key to decrypt the Authenticator
1. TGS checks that the client has access to the requested service (using AD)
1. TGS sends to client:
	- Client/Server Ticket, encrypted with the Service Server's (SS) private key (the TGS has the SS's private keys)
	- Client/Server Session Key, encrypted with the TGS Session Key

Now the client has enough information to access the Service Server.

#### Stage 3 - Accessing the Service Server

1. Client sends to SS:
	- Client/Server Ticket (still encrypted with service's private key)
	- A new Authenticator, encrypted using Client/Server Session Key
1. SS decrypts the Client/Server Ticket using its private key
1. SS uses the Client/Server Ticket to decrypt the Authenticator
1. SS sends to client a message with the timestamp encoded in the Authenticator, encrypted with the Client/Server Ticket
1. This verifies to the client that the SS can be trusted and is willing to service the client

Now the client and the SS can interact.


## Active Directory

- <https://en.wikipedia.org/wiki/Active_Directory>
- Domain controller (DC) authenticates and authorises users and computers in the domain
- AD implements LDAP (lightweight directory access protocol) (not 100%, there are exceptions)
- Stores network objects:
	- resources (machines, printers)
	- security principles (user/computer accounts and groups)
- Objects have many different attributes
- Schema can be extended and modified by administrators
- Hierarchical:
	- A domain is a group of network objects
	- A tree is a collection of domains and domain trees, linked by a trust hierarchy
	- A forest is a collection of trees with a common schema, structure and configuration - used as the security boundary
	- Objects in a domain can be grouped into Organizational Units (OUs). OUs give hierarchy to a domain. OUs should be used for structure instead of domains or sites.
	- Duplicate usernames are an issue. Can't have duplicate usernames in a single domain. This is why you get names like `DOMAIN\scottbe123`.
	- OUs aren't used for access permissions (specific to AD, other directory services support this). Shadow groups are used for this, usually via third party tooling to map a group to an OU.
- Physical structure:
	- Sites are common across the forest - independent of the domain/OU structure
	- Sites are physical groupings based on 1+ IP subnets
	- Include concept of connections between sites
	- Sites are used to control replication between domain controllers and refering clients to the nearest DC
	- AD info is replicated across peer DCs, each DC has a copy of the AD
	- AD uses DNS and TCP/IP, each connection -> link has a cost (speed - the type of connection). This determines the network topology and replication strategy
- Replication:
	- Generally networks using AD will have more than one DC for failover
	- DCs should be single-purpose - other services on the machine can interfere with AD
	- virtualisation can help with reducing hardware costs
- Applications can access AD features using COM interfaces - Active Directory Service Interfaces
- Trusts:
	- Allow users in one domain to access resources in another. Lots of different trust types.
	- There are also forest-level trusts
- Interoperability with *nix systems can be done via LDAP but that doesn't include all of the features of AD. There are third party AD integration applications including Samba which can act as a DC.
- Group policy:
	- <https://en.wikipedia.org/wiki/Group_Policy>
	- centralised management of AD configuration
	- Local Group Policy (LGPO) is a version that enables GPO management in non-domain environments
	- Controls what users are allows to do on a system, for example password policies, RDP configuration, block access to Task Manager
	- GPOs are pushed to computers using Active Directory
	- GPOs are refreshed every 90 minutes + random 30m offset
	- GPOs on DCs are refreshed every 5 minutes
	- Windows 8 clients can have forced GPO updates (per OU) running the update within 10 minutes, with a random offset (avoid spiked load on the DC)
	- Processed in order: local -> site -> domain -> OU
	- policies use inheritance
- [LDAP](https://en.wikipedia.org/wiki/Lightweight_Directory_Access_Protocol) - <https://tools.ietf.org/html/rfc4511>:
     - provides an interface for querying AD - [LDAP Query Basics (TechNet)](https://technet.microsoft.com/en-us/library/aa996205(v=exchg.65).aspx)
     - can also manipulate data and perform authentication


