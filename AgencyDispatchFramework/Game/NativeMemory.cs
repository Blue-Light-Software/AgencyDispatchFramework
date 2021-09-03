using System;
using System.Diagnostics;

namespace AgencyDispatchFramework.Game
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Copyright (C) 2015 crosire & contributors
    /// 
    /// This software is  provided 'as-is', without any express  or implied  warranty. In no event will the
    /// authors be held liable for any damages arising from the use of this software.
    /// Permission  is granted  to anyone  to use  this software  for  any  purpose,  including  commercial
    /// applications, and to alter it and redistribute it freely, subject to the following restrictions:
    /// 
    ///     1. The origin of this software must not be misrepresented; you must not claim that you  wrote the
    ///         original  software. If you use this  software  in a product, an  acknowledgment in the product
    ///         documentation would be appreciated but is not required.
    ///     2. Altered source versions must  be plainly  marked as such, and  must not be  misrepresented  as
    ///         being the original software.
    ///     3. This notice may not be removed or altered from any source distribution.
    /// </remarks>
    /// <seealso cref="https://github.com/crosire/scripthookvdotnet#license"/>
    internal static class NativeMemory
    {
        /// <summary>
		/// Searches the address space of the current process for a memory pattern.
		/// </summary>
		/// <param name="pattern">The pattern.</param>
		/// <param name="mask">The pattern mask.</param>
		/// <returns>The address of a region matching the pattern or <c>null</c> if none was found.</returns>
		public static unsafe byte* FindPattern(string pattern, string mask)
        {
            ProcessModule module = Process.GetCurrentProcess().MainModule;
            return FindPattern(pattern, mask, module.BaseAddress, (ulong)module.ModuleMemorySize);
        }

        /// <summary>
		/// Searches the specific address space of the current process for a memory pattern.
		/// </summary>
		/// <param name="pattern">The pattern.</param>
		/// <param name="mask">The pattern mask.</param>
		/// <param name="startAddress">The address to start searching at.</param>
		/// <param name="size">The size where the pattern search will be performed from <paramref name="startAddress"/>.</param>
		/// <returns>The address of a region matching the pattern or <c>null</c> if none was found.</returns>
		public static unsafe byte* FindPattern(string pattern, string mask, IntPtr startAddress, ulong size)
        {
            ulong address = (ulong)startAddress.ToInt64();
            ulong endAddress = address + size;

            for (; address < endAddress; address++)
            {
                for (int i = 0; i < pattern.Length; i++)
                {
                    if (mask[i] != '?' && ((byte*)address)[i] != pattern[i])
                        break;
                    else if (i + 1 == pattern.Length)
                        return (byte*)address;
                }
            }

            return null;
        }
    }
}
