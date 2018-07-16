
## How to build RhinoLink

RhinoLink has several buildable components, including documentation. This document covers building everything but the documentation.

#### Prerequisites
* Version 11.0 or greater of Mathematica or Wolfram Desktop
* Version 6 or greater of Rhinoceros
* Visual Studio (eg, Community Edition)

#### Building RhinoLink
* Start Visual Studio and open the project file `RhinoLink\VisualStudioBuild\RhinoLink.sln`.
* Choose Build > Rebuild Solution.

#### Installing RhinoLink
* In Mathematica, run `PacletInstall["your\path\RhinoLink\RhinoLink"]`.
* Load the RhinoLink package with `Get["RhinoLink"]`.
* Install RhinoLink components by evaluating `InstallRhinoPlugin[]`.

#### Running RhinoLink
* Start Rhino and run the command `WolframConnect`.
* Start Mathematica, create a new notebook, and set the notebook's kernel to RhinoAttach by choosing Evaluation > Notebook's Kernel > RhinoAttach.
* Test if RhinoLink is running by evaluating `RhinoDocObjects[]`. It should return an empty list (or a non-empty list if you have a non-empty Rhino document).
* In the Help Browser (Help > Wolfram Documentation), search for RhinoLink to bring up the guide page, which contains links to the complete documentation.
