if 'env_wrapper' not in globals():
    raise ImportError("'env_wrapper' is not defined, this script MUST be executed via PixelPool's CLI")
elif 'printc' not in globals():
    raise ImportError("'printc' is not defined, this script MUST be executed via PixelPool's CLI")

env_wrapper: any = globals()['env_wrapper']

def printc(message: str) -> None:
    """
    Print a message with color codes to the C# console.

    Defined colors (ColorLog.cs):

    \n**&0** -> Gray *(default)*
    \n**&1** -> DarkGray
    \n**&2** -> White
    \n**&3** -> Black *(invisible on black background!)*

    \n***Greens***
    \n**&q** -> Green
    \n**&w** -> DarkGreen

    \n***Cyans/Blues***
    \n**&a** -> Cyan
    \n**&s** -> DarkCyan
    \n**&d** -> Blue
    \n**&f** -> DarkBlue

    \n***Reds/Magentas***
    \n**&z** -> Magenta
    \n**&x** -> DarkMagenta
    \n**&c** -> Red
    \n**&v** -> DarkRed

    \n***Yellows:***
    \n**&t** -> Yellow
    \n**&y** -> DarkYellow
    """
    globals()['printc'](message)