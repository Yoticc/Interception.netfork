Interception .net fork
---------------------
The C# fork of the software side of the [oblitum/interception](https://github.com/oblitum/Interception) driver. \
Faster and more efficient than alterantives: [jasonpang/Interceptor](https://github.com/jasonpang/Interceptor), [Nekiplay/MacrosAPI-v3](https://github.com/Nekiplay/MacrosAPI-v3).\
Doesn't allocate memory in the managed heap at all, only `stackalloc` and `Marshal.AllocCoTaskMem()`. \
There is no need for the dependency on interception.dll that alternative solutions have. \
\
This library is a complete rethinking of the approach to the software side of the driver, but if you want to use bare implementation rather than fork, you can take a look at the [Yoticc/Interception.net](https://github.com/Yoticc/Interception.net). 

Changes
-------
The connection to the driver is established after first call in Interception and is never disposed of. \
All filters are now on the software side, not the driver side. \
Has custom checks on down and press. \
No methods to get the device filter and its HWID.

Samples
-------
Can be found at [samples](https://github.com/Yoticc/Interception.netfork/tree/master/Samples/Samples)

Requirements for use
----------------
Installed [interception driver](https://github.com/oblitum/Interception/releases/tag/v1.0.1)
