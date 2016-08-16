# mwobdec
MetroWerks BeOS PowerPC object code format decompiler

This decompiler takes PowerPC Object files from the old MetroWerks compiler used under BeOS PowerPC, and breaks them down in to chunks of logic. The Library format is documented - in so much as I do have a document, but only a printout. The format is used by the BeOS compiler to create object files that are linked in to a PEF binary (native format that BeOS PowerPC uses.) As PEF is pretty much undocumented, this will allow an assembler to be written for PowerPC code under BeOS. This is not something that BeOS ever had before (it used to use the C compiler and inline assembly.) There is absolutely no reason for this project, but I started it about 10 years ago and always wanted to finish it off (this is all new code, but the original wasn't much farther along.) 

#Aims
De-Re-compiler that understands the entire format of a MW PowerPC library and can take the raw parts and compile them in to an identical PowerPC lib. Once We can do that, we can then worry about the assembler (using http://sun.hasenbraten.de/vasm/ seems like a possible fit.) 

#NB
This is a fun little lunch time project. It's not a serious endevour. Please don't expect too much ;-)