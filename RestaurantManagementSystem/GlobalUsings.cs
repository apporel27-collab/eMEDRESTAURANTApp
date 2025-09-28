// Global using directives

// ASP.NET Core MVC - Core components
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Mvc.Filters;
global using Microsoft.AspNetCore.Mvc.Routing;
global using Microsoft.AspNetCore.Mvc.ViewFeatures;
global using Microsoft.AspNetCore.Mvc.Rendering;
global using Microsoft.AspNetCore.Mvc.ModelBinding;
global using Microsoft.AspNetCore.Mvc.Abstractions;
global using Microsoft.AspNetCore.Mvc.Infrastructure;
global using Microsoft.AspNetCore.Mvc.ApplicationModels;
global using Microsoft.AspNetCore.Mvc.RazorPages;
global using Microsoft.AspNetCore.Mvc.TagHelpers;

// ASP.NET Core MVC - Attributes
global using Microsoft.AspNetCore.Mvc.ActionConstraints;
global using Microsoft.AspNetCore.Mvc.ApiExplorer;
global using Microsoft.AspNetCore.Mvc.Authorization;
global using Microsoft.AspNetCore.Mvc.Controllers;
global using Microsoft.AspNetCore.Mvc.Cors;
global using Microsoft.AspNetCore.Mvc.DataAnnotations;
global using Microsoft.AspNetCore.Mvc.Formatters;
global using Microsoft.AspNetCore.Mvc.Localization;
global using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
global using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
global using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
global using Microsoft.AspNetCore.Mvc.Razor;
global using Microsoft.AspNetCore.Mvc.Razor.Compilation;
global using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
global using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
// Removed duplicate - already defined above
// global using Microsoft.AspNetCore.Mvc.Routing;
global using Microsoft.AspNetCore.Mvc.ViewComponents;
global using Microsoft.AspNetCore.Mvc.ViewEngines;
global using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;

// ASP.NET Core Authorization and Authentication
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Authentication;
global using Microsoft.AspNetCore.Authentication.Cookies;

// ASP.NET Core Infrastructure
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Http.Features;
global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.ResponseCaching;
global using Microsoft.AspNetCore.Routing;
global using Microsoft.AspNetCore.StaticFiles;
global using Microsoft.AspNetCore.WebUtilities;
global using IWebHostEnvironment = Microsoft.AspNetCore.Hosting.IWebHostEnvironment;

// Microsoft Extensions
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.Primitives;
global using Microsoft.Extensions.FileProviders;
global using Microsoft.Extensions.ObjectPool;

// Data Access
global using Microsoft.Data.SqlClient;
// Remove System.Data.SqlClient to avoid ambiguous references
// global using System.Data.SqlClient;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Metadata;
global using Microsoft.EntityFrameworkCore.Migrations;
global using Microsoft.EntityFrameworkCore.Storage;
global using Microsoft.EntityFrameworkCore.Infrastructure;
global using System.Data;
global using System.Data.Common;

// System Namespaces
global using System;
global using System.Collections;
global using System.Collections.Generic;
global using System.Collections.Concurrent;
global using System.ComponentModel;
global using System.ComponentModel.DataAnnotations;
global using System.ComponentModel.DataAnnotations.Schema;
global using System.Diagnostics;
global using System.Diagnostics.CodeAnalysis;
global using System.Globalization;
global using System.IO;
global using System.Linq;
global using System.Linq.Expressions;
global using System.Net;
global using System.Net.Http;
global using System.Reflection;
global using System.Runtime.CompilerServices;
global using System.Security.Claims;
global using System.Security.Cryptography;
global using System.Security.Principal;
global using System.Text;
global using System.Text.Encodings.Web;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Text.RegularExpressions;
global using System.Threading;
global using System.Threading.Tasks;

// Add the model namespace to make all models accessible
global using RestaurantManagementSystem.Models;
global using RestaurantManagementSystem.ViewModels;
global using RestaurantManagementSystem.Data;
global using RestaurantManagementSystem.Services;
global using RestaurantManagementSystem.Middleware;
