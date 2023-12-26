/// This pulls from various resources, but primarily Scott Walschin's blog.
///
/// See https://fsharpforfunandprofit.com/posts/concurrency-async-and-parallel/
///
/// The docs are solid, too.
///
/// https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/async-expressions
/// https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/task-expressions
///
/// I'm still trying to grok async vs task. AFAICT, they lack tail calls, and
/// most (all?) .NET code outside of F# uses them.
///
/// Finally, I plan on revisiting this blog post as it's still a little over my
/// head, but I think it covers the state of the art (ish):
///
/// https://medium.com/@eulerfx/f-async-guide-eb3c8a2d180a
open System.Net
open System
open System.IO

let fetchUrl url =
    let req = WebRequest.Create(Uri(url))
    use resp = req.GetResponse()
    use stream = resp.GetResponseStream()
    use reader = new IO.StreamReader(stream)
    let html = reader.ReadToEnd()
    printfn "finished downloading %s" url

let sleepWorkflow = async {
  printfn "Starting sleep workflow at %O" DateTime.Now.TimeOfDay
  do! Async.Sleep 2000
  printfn "Finished sleep workflow at %O" DateTime.Now.TimeOfDay
}

let nestedWorkflow  = async {
  printfn "Starting parent"
  let! childWorkflow = Async.StartChild sleepWorkflow

  // give the child a chance and then keep working
  do! Async.Sleep 100
  printfn "Doing something useful while waiting "

  // block on the child
  let! result = childWorkflow

  // done
  printfn "Finished parent"
}

Async.RunSynchronously nestedWorkflow

let sites = ["http://www.bing.com";
             "http://www.google.com";
             "http://www.amazon.com";
             "http://www.yahoo.com"]

let fetchUrlAsync url =
  async { fetchUrl url }

let betterFetchUrlAsync url =
  async {
    let req = WebRequest.Create(Uri(url))
    use! resp = req.AsyncGetResponse()  // new keyword "use!"
    use stream = resp.GetResponseStream()
    use reader = new IO.StreamReader(stream)
    let html = reader.ReadToEnd()
    printfn "finished downloading %s" url
  }

#time                      // turn interactive timer on
sites
|> List.map fetchUrl        // make a list of async tasks
#time                      // turn timer off

#time                      // turn interactive timer on
sites
|> List.map betterFetchUrlAsync  // make a list of async tasks
|> Async.Parallel          // set up the tasks to run in parallel
|> Async.RunSynchronously  // start them off
#time                      // turn timer off

