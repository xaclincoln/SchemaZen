﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ManyConsole;
using SchemaZen.Library;
using SchemaZen.Library.Command;
using SchemaZen.Library.Models;

namespace SchemaZen.console
{
    public class Script : BaseCommand
    {
        public Script()
            : base(
                "Script", "Generate scripts for the specified database.")
        {
            HasOption(
                "dataTables=",
                "A comma separated list of tables to export data from.",
                o => DataTables = o);
            HasOption(
                "dataTablesPattern=",
                "A regular expression pattern that matches tables to export data from.",
                o => DataTablesPattern = o);

            HasOption(
                "tablesPattern=",
                "A regular expression pattern that matches tables to script.",
                o => TablesPattern = o);
            HasOption(
                "tableHint=",
                "Table hint to use when exporting data.",
                o => TableHint = o);
            HasOption(
                "filterTypes=",
                "A comma separated list of the types that will not be scripted. Valid types: " + Database.ValidTypes,
                o => FilterTypes = o);
        }

        private Logger _logger;
        protected string DataTables { get; set; }
        protected string FilterTypes { get; set; }
        protected string DataTablesPattern { get; set; }
        protected string TablesPattern { get; set; }
        protected string TableHint { get; set; }

        public override int Run(string[] args)
        {
            _logger = new Logger(Verbose);

            if (!Overwrite && Directory.Exists(ScriptDir))
            {
                if (!ConsoleQuestion.AskYN(string.Format("{0} already exists - do you want to replace it", ScriptDir)))
                    return 1;
            }

            var scriptCommand = new ScriptCommand
            {
                ConnectionString = ConnectionString,
                DbName = DbName,
                Pass = Pass,
                ScriptDir = ScriptDir,
                ScriptFile = this.ScriptFile,
                Server = Server,
                User = User,
                Logger = _logger,
                Overwrite = Overwrite
            };

            var filteredTypes = HandleFilteredTypes();
            var namesAndSchemas = HandleDataTables(DataTables);

            try
            {
                scriptCommand.Execute(namesAndSchemas, DataTablesPattern, TablesPattern, TableHint, filteredTypes);
            }
            catch (Exception ex)
            {
                throw new ConsoleHelpAsException(ex.Message);
            }
            return 0;
        }

        private List<string> HandleFilteredTypes()
        {
            var filteredTypes = FilterTypes == null ? new List<string>() : FilterTypes.Split(',').ToList();

            var anyInvalidType = false;
            foreach (var filterType in filteredTypes)
            {
                if (!Database.Dirs.Contains(filterType))
                {
                    _logger.Log(TraceLevel.Warning, string.Format("{0} is not a valid type.", filterType));
                    anyInvalidType = true;
                }
            }

            if (anyInvalidType)
            {
                _logger.Log(TraceLevel.Warning, string.Format("Valid types: {0}", Database.ValidTypes));
            }

            return filteredTypes;
        }

        private Dictionary<string, string> HandleDataTables(string tableNames)
        {
            var dataTables = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(tableNames))
            {
                foreach (var value in tableNames.Split(','))
                {
                    var schema = "dbo";
                    var name = value;
                    if (value.Contains("."))
                    {
                        schema = value.Split('.')[0];
                        name = value.Split('.')[1];
                    }

                    dataTables[name] = schema;
                }
            }
            return dataTables;
        }
    }
}