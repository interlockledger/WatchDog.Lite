// ******************************************************************************************************************************
//  
// Copyright (c) 2022 InterlockLedger Network
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met
//
// * Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer.
//
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
//
// * Neither the name of the copyright holder nor the names of its
//   contributors may be used to endorse or promote products derived from
//   this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES, LOSS OF USE, DATA, OR PROFITS, OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
// ******************************************************************************************************************************

using Microsoft.Net.Http.Headers;

using System.Net.Mime;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE0058 // Expression value is never used

namespace InterlockLedger.WatchDog;

public static class HttpExtensions
{

    public const string TOO_BIG = "[TOO BIG]";
    public static async Task<string> RenderBodyAsync(this Stream stream, string? contentType, long? contentLength) {
        stream.Position = 0;
        if (contentType.HasTextualContentType()) {
            if (contentLength >= ushort.MaxValue || stream.Length >= ushort.MaxValue)
                return TOO_BIG;
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync().ConfigureAwait(false);
        } else
            return stream.ReadBytes(Math.Min(999, (int)stream.Length)).ToSafeBase64();
    }

    public static bool ContentTypeIn(this string contentType, params string[] contentTypes) =>
        MediaTypeHeaderValue.TryParse(contentType, out var mt)
        && (contentTypes.Safe().Any(s => mt.MediaType.Equals(s, StringComparison.OrdinalIgnoreCase)) ||
            contentTypes.Select(s => s.Split('/').Last()).Any(s => mt.Suffix.Equals(s, StringComparison.OrdinalIgnoreCase)));

    public static bool HasTextualContentType(this string? contentType) =>
        contentType.Safe().ContentTypeIn(MediaTypeNames.Text.Plain,
                                         MediaTypeNames.Text.Xml,
                                         MediaTypeNames.Application.Xml,
                                         MediaTypeNames.Application.Json);
}
