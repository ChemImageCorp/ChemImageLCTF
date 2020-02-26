// <copyright file="GlobalSuppressions.cs" company="ChemImage Corporation">
// Copyright (c) ChemImage Corporation. All rights reserved.
// </copyright>

/* This file is used by Code Analysis to maintain SuppressMessage
   attributes that are applied to this project.
   Project-level suppressions either have no target or are given
   a specific target and scoped to a namespace, type, member, etc.*/

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1124:Do not use regions", Justification = "Regions are useful. This rule is draconian.", Scope = "module")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Not ready for localizing strings yet.", Scope = "module")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1200:Using directives should be placed correctly", Justification = "Unnecessary rule. Nothing more correct about putting it inside or outside.")]