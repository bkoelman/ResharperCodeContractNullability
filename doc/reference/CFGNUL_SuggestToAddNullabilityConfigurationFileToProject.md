# ResharperCodeContractNullability configuration

## Activate configuration (VS Extension only)
If you are using the Visual Studio Extension edition (not the NuGet package), you need to manually perform the following additional steps after creating the **ResharperCodeContractNullability.config** file.

In Visual Studio 2015 Update 3 and newer:

1. Select the file in **Solution Explorer**.
2. In the Properties window set the value for **Build Action** to **AdditionalFiles**.

In older versions of Visual Studio 2015:

1. Right click the project in **Solution Explorer** and select **Unload Project**. If you are asked to save changes, click **Yes**.
2. Right click the unloaded project in **Solution Explorer** and select **Edit *ProjectName*.csproj**.
3. Locate the following item in the project file.

    ```csharp
    <None Include="ResharperCodeContractNullability.config" />
    ```

4. Change the definition to the following.

    ```csharp
    <AdditionalFiles Include="ResharperCodeContractNullability.config" />
    ```

5. Save and close the project file.
6. Right click the unloaded project in **Solution Explorer** and select **Reload Project**.

## Settings explained

* disableReportOnNullableValueTypes (default: 'false')
  set to 'true' to stop reporting on nullable value types
* typeHierarchyReportMode (default: AtHighestSourceInTypeHierarchy)
  set to 'EverywhereInTypeHierarchy' to restore behavior of versions up to 1.0.7
  set to 'AtTopInTypeHierarchy'
