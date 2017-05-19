# Prototype of F# compiler with experimental support for traits

Traits add ad hoc polymorphism to the F# language. They are comparable to Haskell's typeclasses, Swift's protocols, Scala's implicits and Rust's traits.

*This is not my work, I am just maintaining the prototype built by the people mentioned in [fsconcepts.md](examples/fsconcepts.md)!*

For more information about the F# compiler itself, please have a look at the readme on the main repo (or the master branch, for that matter).

## How can I play with this?

Clone the traits branch from this repo, then run:

    build.cmd proto
    build.cmd net40

(Others may work, I haven't tested.)

You should then have a compiler and working fsi in the release/net40 folder.

There are a number of examples in the [examples](examples) folder, I usually just play around with those from an fsi instance.

I haven't tried any of the VS integration etc.

