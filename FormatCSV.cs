﻿// Name:        FormatCSV.cs
// Description: Text file import/export for localization
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2012 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.Text;

using IfcDoc.Schema.DOC;

namespace IfcDoc
{
    internal class FormatCSV : IDisposable
    {
        string m_filename;
        DocProject m_project;
        string[] m_locales;

        public FormatCSV(string filename)
        {
            this.m_filename = filename;
        }

        public DocProject Instance
        {
            get
            {
                return this.m_project;
            }
            set
            {
                this.m_project = value;
            }
        }
        
        /// <summary>
        /// Optional list of locales to import/export for names and descriptions.
        /// </summary>
        public string[] Locales
        {
            get
            {
                return this.m_locales;
            }
            set
            {
                this.m_locales = value;
            }
        }

        public void Load()
        {
            // prepare map
            Dictionary<string, DocObject> map = new Dictionary<string, DocObject>();
            foreach (DocSection docSection in this.m_project.Sections)
            {
                foreach (DocSchema docSchema in docSection.Schemas)
                {
                    foreach (DocEntity docEntity in docSchema.Entities)
                    {
                        map.Add(docEntity.Name, docEntity);
                    }
                    foreach (DocType docType in docSchema.Types)
                    {
                        map.Add(docType.Name, docType);
                    }
                }
            }

            // use tabs for simplicity
            using (System.IO.StreamReader reader = new System.IO.StreamReader(this.m_filename))
            {
                string headerline = reader.ReadLine();
                string[] headercols = headerline.Split(new char[]{'\t'}, StringSplitOptions.RemoveEmptyEntries);

                string blankline = reader.ReadLine(); // expect blank line
                
                // first column is name that identifies definition                

                string[] locales = new string[headercols.Length];
                string[] fields = new string[headercols.Length];
                for(int icol = 0; icol < headercols.Length; icol++)
                {
                    string col = headercols[icol];

                    int popen = col.IndexOf('(');
                    int pclos = col.IndexOf(')');
                    if(popen > 0 && pclos > popen)
                    {
                        locales[icol] = col.Substring(popen + 1, pclos - popen - 1);
                        fields[icol] = col.Substring(0, popen);
                    }
                }

                // now rows
                while (!reader.EndOfStream)
                {
                    string rowdata = reader.ReadLine();

                    string[] rowcells = rowdata.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    if (rowcells.Length > 1)
                    {
                        DocObject docObj = null;
                        string identifier = rowcells[0];

                        if (map.TryGetValue(identifier, out docObj))
                        {
                            for(int i = 1; i < rowcells.Length && i < headercols.Length; i++)
                            {
                                if (locales[i] != null)
                                {
                                    // find existing
                                    DocLocalization docLocalization = null;
                                    foreach (DocLocalization docLocal in docObj.Localization)
                                    {
                                        if (docLocal.Locale.Equals(locales[i]))
                                        {
                                            docLocalization = docLocal;
                                            break;
                                        }
                                    }

                                    // create new
                                    if (docLocalization == null)
                                    {
                                        docLocalization = new DocLocalization();
                                        docLocalization.Locale = locales[i];
                                        docObj.Localization.Add(docLocalization);
                                    }

                                    string value = rowcells[i];
                                    if (value != null && value.StartsWith("\"") && value.EndsWith("\""))
                                    {
                                        // strip quotes
                                        value = value.Substring(1, value.Length - 2);
                                    }

                                    // update info
                                    switch (fields[i])
                                    {
                                        case "Name":
                                            docLocalization.Name = value;
                                            break;

                                        case "Description":
                                            docLocalization.Documentation = value;
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void WriteList(System.IO.StreamWriter writer, SortedList<string, DocObject> sortlist)
        {
            foreach (string key in sortlist.Keys)
            {
                DocObject docEntity = sortlist[key];

                writer.Write(docEntity.Name);

                if (this.m_locales != null)
                {
                    foreach (string locale in this.m_locales)
                    {
                        string localname = "";
                        string localdesc = "";

                        foreach (DocLocalization docLocal in docEntity.Localization)
                        {
                            if (docLocal.Locale.Equals(locale))
                            {
                                localname = docLocal.Name;
                                localdesc = docLocal.Documentation;
                                break;
                            }
                        }

                        writer.Write("\t");
                        writer.Write(localname);
                        writer.Write("\t");
                        writer.Write(localdesc);
                    }
                }

                writer.WriteLine();

            }
        }

        public void Save()
        {
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(this.m_filename))
            {
                // header
                writer.Write("Name");
                if (this.m_locales != null)
                {
                    foreach (string locale in this.m_locales)
                    {
                        writer.Write("\tName(");
                        writer.Write(locale);
                        writer.Write(")\tDescription(");
                        writer.Write(locale);
                        writer.Write(")");
                    }
                }
                writer.WriteLine();

                // blank line separates row data
                writer.WriteLine();

                // split into two lists alphabetically for easier translation
                SortedList<string, DocObject> sortlistEntity = new SortedList<string, DocObject>();
                SortedList<string, DocObject> sortlistType = new SortedList<string, DocObject>();

                // rows
                foreach (DocSection docSection in this.m_project.Sections)
                {
                    foreach (DocSchema docSchema in docSection.Schemas)
                    {
                        foreach (DocEntity docEntity in docSchema.Entities)
                        {
                            sortlistEntity.Add(docEntity.Name, docEntity);
                        }

                        foreach (DocType docEntity in docSchema.Types)
                        {
                            sortlistType.Add(docEntity.Name, docEntity);
                        }
                    }
                }

                WriteList(writer, sortlistEntity);
                WriteList(writer, sortlistType);
            }
        }


        #region IDisposable Members

        public void Dispose()
        {           
        }

        #endregion
    }
}
