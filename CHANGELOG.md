# Codecepticon Changelog

## v1.2.0

* `[Update]` Removed the `signtool.exe` dependency and are now natively signing executables. The code was taken & customised from https://github.com/Danielku15/SigningServer, under MIT License - original author is Danielku15.

## v1.1.0

* `[New]` Module: Implement the `sign` module, to enable creating self-signed certificates and using any given certificate to sign an executable. This functionality is using `signtool.exe`.

## v1.0.3

* `[Fix]` C#: Ensure that Delegate function/declarations are also renamed.

## v1.0.2

* `[New]` Mapping: Added checkbox to "Match Exact Word" when searching within the document.
* `[Fix]` C#: Ensure that Seatbelt commands are renamed properly.

## v1.0.1

* `[Fix]` PowerShell: Ensure that variables referenced as `${name}` are also expanded.
* `[Fix]` C#: Ensure that SharpView commands are renamed properly.