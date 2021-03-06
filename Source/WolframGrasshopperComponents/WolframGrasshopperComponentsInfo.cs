﻿using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace WolframGrasshopperComponents
{
    public class WolframGrasshopperComponentsInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "WolframComponents";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                System.Resources.ResourceManager temp = new System.Resources.ResourceManager("WolframGrasshopperComponents.Resources", typeof(WolframGrasshopperComponentsInfo).Assembly);
                object obj = temp.GetObject("wolfieIcon");
                return ((System.Drawing.Bitmap)(obj));
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("{01232c5d-6656-4c8d-ace0-6625841871e0}");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "Wolfram Research, Inc.";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "";
            }
        }
    }
}
