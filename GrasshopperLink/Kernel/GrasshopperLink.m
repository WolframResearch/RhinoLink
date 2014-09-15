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

(* TODO: GHDeploy needs an Initialization option, to specify M code to run before the function is called. Not supported yet. *)
Options[GHDeploy] = {SaveDefinitions -> True, Initialization -> None}


(* Maybe the param spec should be "Text" -> {name, nick, desc, etc...}. Or maybe options for all values? *)
GHDeploy[name_String, func_, inputSpec:{{_String, _String, _String, _String, __}...}, outputSpec:{_String, _String, _String, _String, _}, OptionsPattern[]] :=
    Module[{codeString, asmPath},
        codeString =
            TemplateApply[
                FileTemplate[FileNameJoin[{$thisPacletDir, "Files", "Component.cs"}]],
                <|"Name" -> name, "Nickname" -> name,  "Category" -> "Wolfram",
                  "Subcategory" -> "", "Description" -> "Reverses a string",
                  "GUID" -> CreateUUID[],
                  "RegisterInputParams" -> StringJoin @@ (addParam /@ inputSpec), 
                  "RegisterOutputParams" -> StringJoin @@ addParam[outputSpec],
                  "SolveInstance" -> solveInstance[func, TrueQ[OptionValue[SaveDefinitions]], inputSpec, outputSpec]
                |>
            ];
Global`code = codeString;
        asmPath = generateComponentAssembly[name, codeString];
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
    
addParam[{"Object", name_String, nickname_String, description_String, accessType_, default:_:None}] :=
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



solveInstance[func_, saveDefs:(True | False), inputSpec:{{_String, _String, _String, _String, __}...}, outputSpec:{_String, _String, _String, _String, _}] :=
    Module[{inputTypes, code},
        inputTypes = userTypeToNativeType /@ First /@ inputSpec;
               
        code = StringJoin[MapIndexed[argDeclaration, inputTypes]];
        (* All components will have this one final arg declaration, for the optional link arg. *)
        code = code <> "LinkType linkType = null;\n";
        code = code <> StringJoin[getData /@ Range[Length[inputTypes]]];
        (* All components will have this one final data getter, for the optional link arg. *)
        code = code <> "DA.GetData(" <> ToString[Length[inputSpec]] <> ", ref linkType);\n";
        code = code <> callWolframEngine[func, Length[inputTypes], saveDefs];
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
userTypeToNativeType["Object"] = "object"

argDeclaration["string", {index_Integer}] := "string arg" <> ToString[index] <> " = null;\n"
argDeclaration["int", {index_Integer}] := "int arg" <> ToString[index] <> " = 0;\n"
argDeclaration["double", {index_Integer}] := "double arg" <> ToString[index] <> " = 0.0;\n"
argDeclaration["bool", {index_Integer}] := "bool arg" <> ToString[index] <> " = false;\n"
argDeclaration["object", {index_Integer}] := "object arg" <> ToString[index] <> " = null;\n"
argDeclaration["ExprType", {index_Integer}] := "ExprType arg" <> ToString[index] <> " = null;\n"

getData[index_Integer] := "if (!DA.GetData(" <> ToString[index-1] <> ", ref arg" <> ToString[index] <> ")) { return; }
                           if (arg" <> ToString[index] <> " == null) return;\n"


callWolframEngine[func_, argCount_Integer, saveDefs:(True | False)] :=
    Module[{defs, code},
        code = "IKernelLink link = linkType != null ? linkType.Value : KernelLinkProvider.Link;\n";
        If[saveDefs,
            (* NOTE: ugly call into private CloudObject functionality. Fix. *)
            defs = CloudObject`Private`definitionsToString[Language`ExtendedFullDefinition[func]];
            code = code <>
                     "link.Evaluate(" <> ToString[defs, InputForm] <> ");
                      link.WaitAndDiscardAnswer();\n"
                      (*
                     "link.Evaluate(@\"" <> StringReplace[defs, "\"" -> "\"\""] <> "\");
                      link.WaitAndDiscardAnswer();\n"
                      *)
        ];
        (* TODO: Rather than composing Exprs, wouldn't it be eaiser to use ml.Put() ? 
           IN FACT, the Expr-building won't work for object arguments. You have to Put().
        *)
        code = code <>
            Switch[Head[func],
                Symbol,
                    "Expr e = new Expr(new Expr(ExpressionType.Symbol, \"" <> ToString[func] <> "\"), new object[]{" <>
                               StringJoin[Riffle[Table["arg"<>ToString[i], {i, 1, argCount}], ","]] <> "});\n",
                Function,
                    "Expr e = new Expr(
                                new Expr(
                                   new Expr(ExpressionType.Symbol, \"ToExpression\"), new object[]{\"" <> ToString[func, InputForm] <> "\"}),
                                       new object[]{" <> StringJoin[Riffle[Table["arg"<>ToString[i], {i, 1, argCount}], ","]] <> "});\n",
                _,
                    Message[GHDeploy::badfunc];
                    $Failed
        ];
        code = code <>
                  "link.Evaluate(e);
                   link.WaitForAnswer();\n"
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
                    "Expr res = link.GetExpr();\n",
                "Object",
                    "object res = link.GetObject();\n",
                "Expr",
                    "Expr res = link.GetExpr();\n",
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
    
    

generateComponentAssembly[componentName_String, code_String] :=
NETBlock[
 Module[{provider, providerOptions, params, compilerResults, err, ct, scode},

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
  params @ ReferencedAssemblies @ Add[FileNameJoin[{$thisPacletDir, "Files", "WolframGrasshopperComponents.gha"}]];
  
  params@GenerateInMemory = False;
  params@OutputAssembly = FileNameJoin[{$TemporaryDirectory, componentName <> ".gha"}];

  compilerResults = provider@CompileAssemblyFromSource[params, {code}];
  
  provider@Dispose[];
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
    
    
End[]

EndPackage[]

