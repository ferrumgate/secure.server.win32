netsh int ipv4 show interfaces
netsh interface ipv4 set address "FerrumGate" static 100.64.0.100 255.255.255.255
route ADD 172.16.0.0 MASK 255.255.255.0 100.64.0.100 IF 32
