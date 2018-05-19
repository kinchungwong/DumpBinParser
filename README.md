# DumpBinParser

C# library for parsing the output of DUMPBIN, a binary analysis tool shipped with Visual Studio.

The parsing code is based on the output of DUMPBIN from Visual Studio 2017. It has not been tested with outputs from earlier versions.

The code is intended to augment the work of SymbolSort, written by Adrian Stone and others: https://github.com/adrianstone55/SymbolSort

The code is in experimental status. Not suitable for production use.

# License

MIT license. See LICENSE for information.

## Design

The solution is divided into a "library" project and a "command line" project. Currently the "command line" project is empty.

The library project consists of several external process invokers.

DUMPBIN can be invoked with many different options; the output format vary a lot between these options. From output parsing perspective, DUMPBIN should be considered as a multiplexing of several utilities.

Currently the parsing of the following options are implemented:

* ```DUMPBIN /EXPORTS```
* ```DUMPBIN /DISASM```

The exports list provides mapping between decorated name (those ugly strings) and the human-readable function prototype.

The disassembly provides function call graph.

