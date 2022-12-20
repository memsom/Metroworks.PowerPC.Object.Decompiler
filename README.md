# mwobdec
MetroWerks BeOS PowerPC object code format decompiler

This decompiler takes PowerPC Object files from the old MetroWerks compiler used under BeOS on PowerPC and breaks them down into chunks of logic. The Library format is documented in so much as I do have a document, but only a printout. The format is used by the BeOS compiler to create object files that are linked in to a PEF binary (the native format that BeOS PowerPC uses). As PEF is pretty much undocumented, this will allow an assembler to be written for PowerPC code under BeOS. This is not something that BeOS ever had before. (It used to use the C compiler and inline assembly.) There is absolutely no reason for this project, but I started it about 10 years ago and always wanted to finish it off. (This is all new code, but the original wasn't much farther along.)

## Aims
De-Re-compiler that understands the entire format of a MW PowerPC library and can take the raw parts and compile them in to an identical PowerPC lib. Once we can do that, we can then worry about the assembler. (Using http://sun.hasenbraten.de/vasm/ seems like a possible fit.)

## NB
This is a fun little lunch time project. It's not a serious endevour. Please don't expect too much. ;-)

## Progress
* Now handles all of the example BeOS PowerPC files that make up part of the RTL.
* Handles BeOS PowerPC .a files. (These are basically a bunch of object files concatenated with an ObjHeader then LibFile structs for each object and a name table tacked on to the bottom of that header, followed by the raw Object files.) The format might be aligned to 4 bytes; I've not got as far as to checking that, but there were no docs about the code format so I had to grok the binary and work out what the structure was.
* Creates a directory tree for the output. This makes looking at the output less complicated.
* Handles a lot of the edge cases with regards to naming of file contents.

If you find my work useful, please consider buying me a coffee. [![Buy me a coffee](https://www.buymeacoffee.com/assets/img/bmc-f-logo.svg)](https://buymeacoff.ee/Bxn0HAtp3)
