This is a command line utility to control IKEA Idasen desks or others that also use one of the LINAK DPG controllers with BLE.

## Preparing
First connect your desk to your computer via bluetooth, see the desk manual for how to do this. 
Then run the program with the 'List' command to get the addresses of your desks:
```
Ikea-Idasen-Control.exe list
```
the output:
```
Please wait, the list of devices is forming

Address                 Name            Status
d4:7e:f5:99:85:0a
ec:02:09:df:8e:d8       Desk 6568       Paired
df:4e:ab:cd:67:e8       LHB-691F176B
```
Use the found addresses next to control your desks. For instance below I will use ec:02:09:df:8e:d8 address of my Desk 6568.

## Movement
To move the desk to a height of 700 mm from the floor use the 'Move' command. Add the exact height in millimeters:
```
Ikea-Idasen-Control.exe move -a ec:02:09:df:8e:d8 700
```
the output:
```
Moving the desk to 700 mm
Current height is 700 mm
```

To move the desk to the 1st favorite position, use the number of the favorite position:
```
Ikea-Idasen-Control.exe move -a ec:02:09:df:8e:d8 m1
```
the output:
```
Moving the desk to 695 mm
Current height is 695 mm
```

## Setting your favorite desk positions
To set your favorite desk position, use the 'Set' command and the number of your favorite position. 
Add the word 'current' to save the current table height or the exact height in millimeters:
```
Ikea-Idasen-Control.exe set -a ec:02:09:df:8e:d8 m3 current
```
the output:
```
Name             Desk 6568
Current height      623 mm
Minimum height      619 mm
Memory position 1   695 mm
Memory position 2  1116 mm
Memory position 3   623 mm
```

```
Ikea-Idasen-Control.exe set -a ec:02:09:df:8e:d8 m3 622
```
the output:
```
Name             Desk 6568
Current height      623 mm
Minimum height      619 mm
Memory position 1   695 mm
Memory position 2  1116 mm
Memory position 3   622 mm
```
To clear your favorite desk position, use the 'Clear' command and the number of your favorite position:
```
Ikea-Idasen-Control.exe clear -a ec:02:09:df:8e:d8 m3
```
the output:
```
Name             Desk 6568
Current height      623 mm
Minimum height      619 mm
Memory position 1   695 mm
Memory position 2  1116 mm
Memory position 3
```

## Get current desk state and settings
The command 'Show' shows you your current desk state and settings:
```
Ikea-Idasen-Control.exe show -a ec:02:09:df:8e:d8
```
the output:
```
Name             Desk 6568
Current height      623 mm
Minimum height      619 mm
Memory position 1   695 mm
Memory position 2  1116 mm
Memory position 3   619 mm
```

## Height calibration
Lower the desk to its lowest position, measure the distance from the floor to the top of the desktop with a tape measure. Set the measured value in the program instead of the standard 650 mm. 
The program will then show you more accurate height values.
```
Ikea-Idasen-Control.exe minheight -a ec:02:09:df:8e:d8 618
```
the output:
```
Name             Desk 6568
Current height      622 mm
Minimum height      618 mm
Memory position 1   694 mm
Memory position 2  1115 mm
Memory position 3   621 mm
```
