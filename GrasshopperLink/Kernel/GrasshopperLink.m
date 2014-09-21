BeginPackage["GrasshopperLink`", {"NETLink`"}]


GHDeploy::usage = ""
$GrasshopperPath::usage = ""
$RhinoCommonPath::usage = ""


Begin["`Private`"]

If[!StringQ[$GrasshopperPath],
    $GrasshopperPath = FileNameJoin[{ParentDirectory[$UserBaseDirectory],"McNeel\\Rhinoceros\\5.0\\Plug-ins\\Grasshopper (b45a29b1-4343-4035-989e-044e8580d9cf)\\0.9.76.0"}]
]
If[!StringQ[$RhinoCommonPath],
    $RhinoCommonPath = "c:\\Program Files (x86)\\Rhinoceros 5\\System\\rhinocommon.dll"
]

$deployDir = FileNameJoin[{ParentDirectory[$UserBaseDirectory],"Grasshopper", "Libraries"}]


$thisPacletDir = ParentDirectory[DirectoryName[$InputFileName]]


(**********************************  GHDeploy  *********************************)

GHDeploy::param:= "The parameter type `1` is not currently supported."

Options[GHDeploy] = {SaveDefinitions -> True, Initialization -> None, "Description" -> "User-generated Wolfram Language computation",  "Icon" -> Automatic}


(* Maybe the param spec should be "Text" -> {name, nick, desc, etc...}. Or maybe options for all values? *)
GHDeploy[name_String, func_, inputSpec:{{_String, _String, _String, _String, __}...}, outputSpec:{_String, _String, _String, _String, _}, OptionsPattern[]] :=
    Module[{codeString, asmPath, saveDefs, initialization, icon, desc},
        {saveDefs, initialization, icon, desc} = OptionValue[{SaveDefinitions, Initialization, "Icon", "Description"}];
        If[icon === Automatic, icon = name];
        (* Fail right away if func is not of the correct form. Must be symbol or pure function. *)
        If[Head[func] =!= Symbol && Head[func] =!= Function,
            Message[GHDeploy::badfunc];
            Return[$Failed]
        ];
        codeString =
            TemplateApply[
                FileTemplate[FileNameJoin[{$thisPacletDir, "Files", "Component.cs"}]],
                <|"Name" -> name, "Nickname" -> name,  "Category" -> "Wolfram",
                  "Subcategory" -> "", "Description" -> desc,
                  "GUID" -> CreateUUID[],
                  "RegisterInputParams" -> StringJoin @@ (addParam /@ inputSpec), 
                  "RegisterOutputParams" -> StringJoin @@ addParam[outputSpec],
                  "SolveInstance" -> solveInstance[func, TrueQ[saveDefs], initialization, inputSpec, outputSpec]
                |>
            ];
Global`code = codeString;
        asmPath = generateComponentAssembly[name, codeString, icon];
        If[StringQ[asmPath],
            deployComponentAssembly[asmPath];
            Null,
        (* else *)
            $Failed
        ]
    ]
    
(* TODO: accessType for all of these (list/item). 
         Issue message on bad default value.
         Coalesce these into a single function; they are almost identical.       
*)

addParam[{"Text", name_String, nickname_String, description_String, accessType_, default:_:None}] :=
    Module[{},
        TemplateApply[StringTemplate["pManager.AddTextParameter(\"`Name`\", \"`Nickname`\", \"`Description`\", `Access` `Default`);\n"],
                <|"Name" -> name, "Nickname" -> nickname, "Description" -> description, "Access" -> "GH_ParamAccess.item",
                  "Default" -> If[StringQ[default], ", " <> ToString[default, InputForm], ""]
                |>
        ]
    ]
    
addParam[{"Integer", name_String, nickname_String, description_String, accessType_, default:_:None}] :=
    Module[{},
        TemplateApply[StringTemplate["pManager.AddIntegerParameter(\"`Name`\", \"`Nickname`\", \"`Description`\", `Access` `Default`);\n"],
                <|"Name" -> name, "Nickname" -> nickname, "Description" -> description, "Access" -> "GH_ParamAccess.item",
                  "Default" -> If[IntegerQ[default], ", " <> ToString[default, InputForm], ""]
                |>
        ]
    ]
    
addParam[{"Number", name_String, nickname_String, description_String, accessType_, default:_:None}] :=
    Module[{},
        TemplateApply[StringTemplate["pManager.AddNumberParameter(\"`Name`\", \"`Nickname`\", \"`Description`\", `Access` `Default`);\n"],
                <|"Name" -> name, "Nickname" -> nickname, "Description" -> description, "Access" -> "GH_ParamAccess.item",
                  "Default" -> If[NumberQ[default], ", " <> ToString[N[default], InputForm], ""]
                |>
        ]
    ]
    
addParam[{"Boolean", name_String, nickname_String, description_String, accessType_, default:_:None}] :=
    Module[{},
        TemplateApply[StringTemplate["pManager.AddBooleanParameter(\"`Name`\", \"`Nickname`\", \"`Description`\", `Access` `Default`);\n"],
                <|"Name" -> name, "Nickname" -> nickname, "Description" -> description, "Access" -> "GH_ParamAccess.item",
                  "Default" -> If[NumberQ[default], ", " <> ToString[N[default], InputForm], ""]
                |>
        ]
    ]
    
addParam[{"Any", name_String, nickname_String, description_String, accessType_, default:_:None}] :=
    Module[{},
        TemplateApply[StringTemplate["pManager.AddGenericParameter(\"`Name`\", \"`Nickname`\", \"`Description`\", `Access` `Default`);\n"],
                <|"Name" -> name, "Nickname" -> nickname, "Description" -> description, "Access" -> "GH_ParamAccess.item",
                  "Default" -> "" (* No support for a default value in AddGenericParameter. *)
                |>
        ]
    ]
        
addParam[{"Expr", name_String, nickname_String, description_String, accessType_, default:_:None}] :=
    Module[{},
        TemplateApply[
            StringTemplate[
                "pManager.AddParameter(new ExprParam(), \"`Name`\", \"`Nickname`\", \"`Description`\", `Access` `Default`);\n"
            ],
            <|"Name" -> name, "Nickname" -> nickname, "Description" -> description, "Access" -> "GH_ParamAccess.item",
              "Default" -> "" (* No support for a default value in AddParameter. *)
            |>
        ]
    ]
    
addParam[{unsupported_String, nickname_String, description_String, accessType_, default:_:None}] :=
    (Message[GHDeploy::param, unsupported]; $Failed)



solveInstance[func_, saveDefs:(True | False), initialization_, 
               inputSpec:{{_String, _String, _String, _String, __}...}, outputSpec:{_String, _String, _String, _String, _}] :=
    Module[{inputTypes, code},
        inputTypes = userTypeToNativeType /@ First /@ inputSpec;
               
        code = StringJoin[MapIndexed[argDeclaration, inputTypes]];
        (* All components will have this one final arg declaration, for the optional link arg. *)
        code = code <> "LinkType linkType = null;\n";
        code = code <> StringJoin[MapIndexed[getData, inputTypes]];
        (* All components will have this one final data getter, for the optional link arg. *)
        code = code <> "DA.GetData(" <> ToString[Length[inputSpec]] <> ", ref linkType);\n";
        code = code <> callWolframEngine[func, Length[inputTypes], saveDefs, initialization];
        code = code <> readResult[First[outputSpec]];
        code
    ]
    
(* DO I NEED THESE? *)
userTypeToNativeType["Text"] = "string"
userTypeToNativeType["Integer"] = "int"
userTypeToNativeType["Number"] = "double"
userTypeToNativeType["Boolean"] = "bool"
userTypeToNativeType["Any"] = "object"
userTypeToNativeType["Expr"] = "ExprType"

argDeclaration["string", {index_Integer}] := "string arg" <> ToString[index] <> " = null;\n"
argDeclaration["int", {index_Integer}] := "int arg" <> ToString[index] <> " = 0;\n"
argDeclaration["double", {index_Integer}] := "double arg" <> ToString[index] <> " = 0.0;\n"
argDeclaration["bool", {index_Integer}] := "bool arg" <> ToString[index] <> " = false;\n"
argDeclaration["object", {index_Integer}] := "object arg" <> ToString[index] <> " = null;\n"
argDeclaration["ExprType", {index_Integer}] := "ExprType arg" <> ToString[index] <> " = null;\n"

getData[type_, {index_Integer}] := 
    Module[{argName = "arg" <> ToString[index], code},
        code = TemplateApply[
            StringTemplate[
                "if (!DA.GetData(`indexMinusOne`, ref `argName`)) return;
                 if (`argName` == null) return;\n"
            ],
            <|"argName" -> argName, "indexMinusOne" -> ToString[index-1]|>
        ];
        If[type == "object",
            code = code <> TemplateApply[StringTemplate["`argName` = `argName`.ScriptVariable();\n"], <|"argName" -> argName|>]
        ];
        code
    ]


callWolframEngine[func_, argCount_Integer, saveDefs:(True | False), initialization_] :=
    Module[{defs, code},
        code = "IKernelLink link = linkType != null ? linkType.Value : Utils.GetLink();\n";
        If[initialization =!= None,
            code = code <>
                     "link.Evaluate(\"ReleaseHold[" <> ToString[initialization, InputForm] <> "]\");
                      link.WaitAndDiscardAnswer();\n"
        ];
        If[saveDefs,
            (* NOTE: ugly call into private CloudObject functionality. Fix. *)
            defs = CloudObject`Private`definitionsToString[Language`ExtendedFullDefinition[func]];
            code = code <>
                     "link.Evaluate(" <> ToString[defs, InputForm] <> ");
                      link.WaitAndDiscardAnswer();\n"
        ];
        code <>
           "link.PutFunction(\"EvaluatePacket\", 1);
            link.PutNext(ExpressionType.Function);
            link.PutArgCount(" <> ToString[argCount] <> ");\n" <>
            Switch[Head[func],
                Symbol,
                   "link.PutSymbol(\"" <> ToString[func] <> "\");",
                Function,
                   "link.PutFunction(\"ToExpression\", 1);
                    link.Put(\"" <> ToString[func, InputForm] <> "\");"
            ] <>  
            StringJoin[Table["link.Put(arg"<>ToString[i]<>");\n", {i, 1, argCount}]] <> 
            "link.WaitForAnswer();\n"
    ]


readResult[outputType_] :=
    Module[{},
        "try {\n" <>
            Switch[outputType,
                "Text",
                    "string res = link.GetString();\n",
                "Integer",
                    "int res = link.GetInteger();\n",
                "Number",
                    "double res = link.GetDouble();\n",
                "Boolean",
                    "bool res = link.GetBoolean();\n",
                "Any",
                    "Object res = null;
                     ILinkMark mark = link.CreateMark();
                     try {
                         res = link.GetObject();
                     } catch (MathLinkException) {
                         link.ClearError();
                         link.SeekMark(mark);
                         Expr ex = link.GetExpr();
                         res = new ExprType(ex);
                     } finally {
                         link.DestroyMark(mark);
                     }\n",
                "Expr",
                    "Expr ex = link.GetExpr();
                     ExprType res = new ExprType(ex);\n",
                 _,
                    Message[GHDeploy::outnotsup, outputType];
                    $Failed
            ] <>
            "DA.SetData(0, res);
             DA.SetData(1, new LinkType(link)); 
         } catch (MathLinkException exc) {
             AddRuntimeMessage(GH_RuntimeMessageLevel.Error, " <> ToString["Unexpected type of result from Wolfram Engine", InputForm] <> ");
             link.ClearError();
             link.NewPacket();
             return;
         }"
    ]
    
    

generateComponentAssembly[componentName_String, code_String, icon_] :=
NETBlock[
 Module[{provider, providerOptions, params, compilerResults, err, ct, scode, resFile},
     
     resFile = generateResourcesFile[icon];
  
          

  providerOptions = NETNew["System.Collections.Generic.Dictionary`2[string, string]"];
  providerOptions@Add["CompilerVersion", "v4.0"];
  provider = NETNew["Microsoft.CSharp.CSharpCodeProvider", providerOptions];
  
  params = NETNew["System.CodeDom.Compiler.CompilerParameters"];
  params @ ReferencedAssemblies @ Add["System.dll"];
  params @ ReferencedAssemblies @ Add["System.Core.dll"];
  params @ ReferencedAssemblies @ Add["System.Drawing.dll"];
  params @ ReferencedAssemblies @ Add["System.Windows.Forms.dll"];
  params @ ReferencedAssemblies @ Add[FileNameJoin[{$GrasshopperPath, "GH_IO.dll"}]];
  params @ ReferencedAssemblies @ Add[FileNameJoin[{$GrasshopperPath, "Grasshopper.dll"}]];
  params @ ReferencedAssemblies @ Add[$RhinoCommonPath];
  params @ ReferencedAssemblies @ Add[FileNameJoin[{$thisPacletDir, "Files", "Wolfram.NETLink.dll"}]];
  params @ ReferencedAssemblies @ Add[FileNameJoin[{$thisPacletDir, "Files", "WolframGrasshopperSupport.dll"}]];
  
  params@EmbeddedResources@Add[resFile];
  
  params@GenerateInMemory = False;
  params@OutputAssembly = FileNameJoin[{$TemporaryDirectory, componentName <> ".gha"}];

  compilerResults = provider@CompileAssemblyFromSource[params, {code}];
  
  provider@Dispose[];
  DeleteFile[resFile];
  
  err = compilerResults@Errors;
  ct = err@Count;
  If[ct > 0,
    scode = StringSplit[code, "\n"]; 
    Print["number of errors = ", ct];
    Print[ 
      err[#]@ErrorText,
        " in line ",
        err[#]@Line,
        StringJoin @ 
       Insert[Characters[scode[[err[#]@Line]]], 
        ToString[\[FilledRightTriangle]], err[#]@Column]
            ] & /@ Range[0, ct - 1];
    Return[$Failed]
   ];

  compilerResults@PathToAssembly
  ]
]


generateResourcesFile[icon_] :=
    NETBlock[
        Module[{iconGraphic, iconData, resWriter, resFile},
            resFile = FileNameJoin[{$TemporaryDirectory, "gh.resources"}];
            (* prepare icon file. *)
            If[Head[icon] === Graphics,
                (* If it's already a Graphics, resize to 24x24. *)
                iconGraphic = ImageResize[icon, {24, 24}],
            (* else *)
                iconGraphic = Graphics[{Text[Style[icon, {Bold, Larger}]]}, ImageSize->{24,24}]
            ];
            iconData = ToCharacterCode[ExportString[iconGraphic, "PNG"]];
            resWriter = NETNew["System.Resources.ResourceWriter", resFile];
            resWriter@AddResource["Icon", MakeNETObject[iconData, "System.Byte[]"]];
            resWriter@Close[];
            resFile
        ]
    ]
 
 
deployComponentAssembly[assemblyPath_String] :=
    Module[{asmName},
        asmName = FileNameTake[assemblyPath];
        If[FileExistsQ[FileNameJoin[{$deployDir, asmName}]],
            DeleteFile[FileNameJoin[{$deployDir, asmName}]]
        ];
        CopyFile[assemblyPath, FileNameJoin[{$deployDir, asmName}]];
        (* Copy needed libraries if they are not already present. *)
        If[!FileExistsQ[FileNameJoin[{$deployDir, "WolframGrasshopperSupport.dll"}]],
            CopyFile[FileNameJoin[{$thisPacletDir, "Files", "WolframGrasshopperSupport.dll"}], FileNameJoin[{$deployDir, "WolframGrasshopperSupport.dll"}]]
        ];
        If[!FileExistsQ[FileNameJoin[{$deployDir, "WolframGrasshopperComponents.gha"}]],
            CopyFile[FileNameJoin[{$thisPacletDir, "Files", "WolframGrasshopperComponents.gha"}], FileNameJoin[{$deployDir, "WolframGrasshopperComponents.gha"}]]
        ];
        If[!FileExistsQ[FileNameJoin[{$deployDir, "Wolfram.NETLink.dll"}]],
            CopyFile[FileNameJoin[{$thisPacletDir, "Files", "Wolfram.NETLink.dll"}], FileNameJoin[{$deployDir, "Wolfram.NETLink.dll"}]]
        ];
        If[!FileExistsQ[FileNameJoin[{$deployDir, "ml32i4.dll"}]],
            CopyFile[FileNameJoin[{$thisPacletDir, "Files", "ml32i4.dll"}], FileNameJoin[{$deployDir, "ml32i4.dll"}]]
        ];
        If[!FileExistsQ[FileNameJoin[{$deployDir, "ml64i4.dll"}]],
            CopyFile[FileNameJoin[{$thisPacletDir, "Files", "ml64i4.dll"}], FileNameJoin[{$deployDir, "ml64i4.dll"}]]
        ]
    ]
    
    
    
(* Called from C# Wolfram script. Sets up the link from a launched kernel that waits for an FE to attach. *)
setupRhinoAttachLink[] :=
    Module[{link},
        debug["Entering setuplinksfromunity"];
        link = LinkCreate["RhinoAttach"];
        If[Head[link] === LinkObject,
            MathLink`AddSharingLink[link, MathLink`SendInputNamePacket -> True]
        ];
        debug["attach: " <> ToString[link]];
        
        debug["at start, previous link was : " <> ToString[$previousLink]];
        If[Head[$previousLink] === LinkObject,
            (* Typically, was already closed in unityDetach[] from last session. *)
            MathLink`RemoveSharingLink[$previousLink];
            LinkClose[$previousLink];
            debug["just closed " <> ToString[$previousLink]]
        ];
        
        (*prepareLinkForUnityToConnect[];*)
        
        $previousLink = $ParentLink;
        debug["link waiting for next time: " <> ToString[link]];
        debug["current link, which wil lbe closed next time " <> ToString[$previousLink]];
        
        (* It is important to make preemptive all the links that could get U-to-M traffic. Otherwise it is easy
           to get deadlock when calling from Wolfram into Unity at the same time a user script is calling
           into Wolfram.
        *)
        MathLink`AddSharingLink[$ParentLink, MathLink`AllowPreemptive -> True];
    ]
    
    
    (* Called from C# Wolfram script *)
setupReaderLink[] :=
    (
        $readerLink = LinkCreate[];
        LinkWrite[$ParentLink, First[$readerLink]];
        LinkConnect[$readerLink];
        NETLink`InstallNET`Private`$nlink = $readerLink;
        NETLink`InstallNET`Private`$uilink = $ParentLink;
    )



End[]

EndPackage[]

