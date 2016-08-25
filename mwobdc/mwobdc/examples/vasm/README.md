#VASM test
This is a simple bit of code that uses a quickly hacked together version of VASM with the PowerPC 
module and standard assembler syntax modules hard coded in. The code is essentially the op codes
from test,c compiled under the mwccppc in Haiku, decompiled using mwdisppc then put in to the bare
minimum of a PowerPC shell assembler file and assembled using the -Fbin switch (raw PowerPC object 
code.) This gives (unsurprisingly) the exact same binary on disk output as the code we extracted 
from the object file we deconstructed (it's 2 op codes, it can't be that different, amirite?)

Next step is to do more tests and to see iw we can stitch together a different test and get the same 
results. Should be fun...