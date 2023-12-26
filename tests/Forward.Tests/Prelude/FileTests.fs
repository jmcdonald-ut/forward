module Forward.Tests.Prelude.FileTests

open NUnit.Framework

[<TestFixture>]
type Tests() =
  let mutable forwardFiles: System.IO.DirectoryInfo = null
  let mutable forwardTestFile1: string = null
  let mutable forwardTestFile2: string = null

  [<OneTimeSetUp>]
  member this.setUpBasicTestFiles() =
    forwardFiles <- System.IO.Directory.CreateTempSubdirectory("fwd_root")
    forwardTestFile1 <- File.combinePaths forwardFiles.FullName "test1.cnf"
    File.writeText "[mysqld]\ninnodb_ft_min_token_size=1\n[client]\nuser=foo\npassword=bar" forwardTestFile1
    forwardTestFile2 <- File.combinePaths forwardFiles.FullName "test2.cnf"
    File.writeText "[mysqld]\ninnodb_ft_min_token_size=1\n[client]\nuser=foo" forwardTestFile2

  [<OneTimeTearDown>]
  member this.tearDownBasicTestFiles() =
    System.IO.Directory.Delete(forwardFiles.FullName, true)

  [<Test>]
  member this.testReadFileLinesIn() =
    let expected =
      [| "[mysqld]"
         "innodb_ft_min_token_size=1"
         "[client]"
         "user=foo"
         "password=bar" |]

    let actual = File.readFileLinesIn forwardTestFile1

    Assert.That(actual, Is.EqualTo(expected))
