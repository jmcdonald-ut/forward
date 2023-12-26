module Forward.Tests.Forward.MySql.ConnectionTests

open NUnit.Framework
open Forward.MySql.Connection

[<TestFixture>]
type BuildConfigTests() =
  let getVariable (variable: string) =
    match variable with
    | "DB_NAME" -> Some "testDb"
    | "DB_HOST" -> Some "db.host.mysql.com"
    | _ -> None

  let mutable forwardFiles: System.IO.DirectoryInfo = null
  let mutable fullOptionsFile: string = null
  let mutable userOnlyOptionsFile: string = null
  let mutable passwordOnlyOptionsFile: string = null
  let mutable noClientOptionsFile: string = null

  [<OneTimeSetUp>]
  member this.setUpBasicTestFiles() =
    let sharedStart: string = "[mysqld]\ninnodb_ft_min_token_size=1"
    forwardFiles <- System.IO.Directory.CreateTempSubdirectory("fwd_mysql_connection")
    fullOptionsFile <- File.combinePaths forwardFiles.FullName "fullOptionsFile.cnf"
    File.writeText (sharedStart + "\n[client]\nuser=testUser\npassword=testPassword") fullOptionsFile
    userOnlyOptionsFile <- File.combinePaths forwardFiles.FullName "userOnlyOptionsFile.cnf"
    File.writeText (sharedStart + "\n[client]\nuser=testUserOnly") userOnlyOptionsFile
    passwordOnlyOptionsFile <- File.combinePaths forwardFiles.FullName "passwordOnly.cnf"
    File.writeText (sharedStart + "\n[client]\npassword=testPasswordOnly") passwordOnlyOptionsFile
    noClientOptionsFile <- File.combinePaths forwardFiles.FullName "noClientOptionsFile.cnf"
    File.writeText sharedStart noClientOptionsFile

  [<OneTimeTearDown>]
  member this.tearDownBasicTestFiles() =
    System.IO.Directory.Delete(forwardFiles.FullName, true)

  [<Test>]
  member this.testValidInput() =
    let expectedConfig: ConnectionConfig =
      { User = "testUser"
        Password = "testPassword"
        DbName = "testDb"
        Host = "db.host.mysql.com" }

    let expected: Result<ConnectionConfig, string> = Ok(expectedConfig)

    let actual: Result<ConnectionConfig, string> =
      buildConfig getVariable [ fullOptionsFile ]

    Assert.That(actual, Is.EqualTo(expected))

  [<Test>]
  member this.testValidInputSpreadAcrossOptionsFiles() =
    let expectedConfig: ConnectionConfig =
      { User = "testUserOnly"
        Password = "testPasswordOnly"
        DbName = "testDb"
        Host = "db.host.mysql.com" }

    let expected: Result<ConnectionConfig, string> = Ok(expectedConfig)

    let actual: Result<ConnectionConfig, string> =
      buildConfig getVariable [ userOnlyOptionsFile; passwordOnlyOptionsFile ]

    Assert.That(actual, Is.EqualTo(expected))

  [<Test>]
  member this.testValidInputAndManyFilesUsesFirstMatches() =
    let expectedConfig: ConnectionConfig =
      { User = "testUser"
        Password = "testPassword"
        DbName = "testDb"
        Host = "db.host.mysql.com" }

    let expected: Result<ConnectionConfig, string> = Ok(expectedConfig)

    let actual: Result<ConnectionConfig, string> =
      buildConfig
        getVariable
        [ fullOptionsFile
          userOnlyOptionsFile
          passwordOnlyOptionsFile
          noClientOptionsFile ]

    Assert.That(actual, Is.EqualTo(expected))

  [<Test>]
  member this.testInvalidFileReturnsError() =
    let invalidPath: string = File.combinePaths forwardFiles.FullName "some.bad.cnf"

    let expected: Result<ConnectionConfig, string> =
      Error(sprintf "%s cannot be found." invalidPath)

    let actual: Result<ConnectionConfig, string> =
      buildConfig getVariable [ invalidPath ]

    Assert.That(actual, Is.EqualTo(expected))
