#!/bin/bash

# Ultra-fast build and run script for Restaurant Management System
echo "🚀 Starting ultra-fast build process..."

# Navigate to project directory
cd /Users/abhikporel/dev/Restaurantapp/RestaurantManagementSystem

# Kill any existing dotnet processes
echo "🛑 Stopping existing processes..."
pkill -f dotnet 2>/dev/null || true

# Skip clean for faster startup - only restore if needed
echo "📦 Quick package check..."
if [ ! -d "bin/Debug" ]; then
    dotnet restore --nologo --verbosity quiet --no-dependencies
fi

# Ultra-fast incremental build
echo "⚡ Ultra-fast incremental build..."
dotnet build \
    --configuration Debug \
    --nologo \
    --verbosity minimal \
    --no-restore \
    --no-dependencies \
    /p:UseSharedCompilation=true \
    /p:BuildInParallel=true \
    /p:MultiProcessorCompilation=true \
    /p:SkipAnalyzers=true \
    /p:RunAnalyzersDuringBuild=false

if [ $? -eq 0 ]; then
    echo "⚡ Build complete! Starting application with turbo mode..."
    
    # Run with maximum performance settings
    export ASPNETCORE_ENVIRONMENT=Development
    export DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER=1
    export DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true
    export DOTNET_GCServer=1
    export DOTNET_GCConcurrent=1
    export DOTNET_GCRetainVM=1
    export DOTNET_ReadyToRun=0
    export DOTNET_TieredPGO=0
    export DOTNET_TC_QuickJitForLoops=1
    
    dotnet run --no-build --no-restore --no-dependencies
else
    echo "❌ Build failed!"
    exit 1
fi