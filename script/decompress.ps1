param (
    [string]$filePath
)

function DecompressFileType30 {
    param ($filePath, $fileStream)

    Write-Host "Decompressing file type 30: $filePath"

    $fileStream.Seek(0x4, [System.IO.SeekOrigin]::Begin) | Out-Null

    $compressdFileLength = New-Object byte[] 4
    $fileStream.Read($compressdFileLength, 0, $compressdFileLength.Length) | Out-Null

    $fileLength = $fileStream.Length - 0x8
    Write-Host "Compressed file length: $fileLength"

    $desktop = [System.Environment]::GetFolderPath("Desktop")
    $xored = New-Object System.Collections.Generic.List[byte]
    for ($i = 0; $i -lt $fileLength; $i++) {
        $currentByte = $fileStream.ReadByte()
        if ($currentByte -eq -1) {
            Write-Host "End of stream reached unexpectedly."
            break
        }
        $xoredByte = $currentByte -bxor 0x72
        $xored.Add([byte]$xoredByte)
    }

    $fileName = [System.IO.Path]::GetFileName($filePath)
    $directory = [System.IO.Path]::GetDirectoryName($filePath)
    $outputPath = [System.IO.Path]::Combine($directory, $fileName)
    $fileStream.Close()
    [System.IO.File]::WriteAllBytes($outputPath, $xored.ToArray())

    Write-Host "Decompression complete for file type 30: $filePath"
}

function DecompressFileType31 {
    param ($filePath, $fileStream)

    Write-Host "Decompressing file type 31: $filePath"

    $fileStream.Seek(0x4, [System.IO.SeekOrigin]::Begin) | Out-Null

    $decompressedFileLengthBytes = New-Object byte[] 4
    $fileStream.Read($decompressedFileLengthBytes, 0, $decompressedFileLengthBytes.Length) | Out-Null
    $decompressedFileLength = [BitConverter]::ToInt32($decompressedFileLengthBytes, 0)
    Write-Host "Decompressed file length: $decompressedFileLength"

    $fileLength = $fileStream.Length - 0x8
    $a0C = 0x0
    $currentByte = 0x0
    $count = 0
    $desktop = [System.Environment]::GetFolderPath("Desktop")
    $fileWordCache = New-Object System.Collections.Generic.List[byte]
    for ($i = 0; $i -lt 0xFEE; $i++) {
        $fileWordCache.Add(0x0)
    }

    $xored = New-Object System.Collections.Generic.List[byte]
    $t1C = 0xFEE
    for ($i = 0; $i -lt $fileLength; $i++) {
        if ($i -eq 0 -or $a0C -eq 0xFF) {
            $count = $a0C -band 1
            $currentByte = $fileStream.ReadByte() -bxor 0x72
            if ($currentByte -eq -1) {
                Write-Host "End of stream reached unexpectedly."
                break
            }
            $a0C = 0xFF00 + $currentByte
            $count = $a0C -band 0x1
        }
        else {
            if (($a0C -band 1) -eq 0) {
                $count = 0

                $currentByte = $fileStream.ReadByte()
                if ($currentByte -eq -1) {
                    Write-Host "End of stream reached unexpectedly."
                    break
                }
                $xoredByte = $currentByte -bxor 0x72

                $currentByte2 = $fileStream.ReadByte()
                if ($currentByte2 -eq -1) {
                    Write-Host "End of stream reached unexpectedly."
                    break
                }
                $xoredByte2 = $currentByte2 -bxor 0x72
                $i++

                $t3 = $xoredByte2 -band 0xF0
                $t3 = $t3 -shl 0x4

                $t6 = $xoredByte -bor $t3
                $t3 = $xoredByte2 -band 0xF
                $xoredByte = $t3 + 0x2
                while ($true) {
                    $t3 = $t6 + $count
                    $t3 = $t3 -band 0xFFF
                    $t4 = $fileWordCache[$t3]

                    $xored.Add([byte]$t4)
                    $count++
                    $t1C++

                    if ($fileWordCache.Count -gt $t1C) {
                        $fileWordCache[$t1C - 1] = [byte]$t4
                    }
                    else {
                        $fileWordCache.Add([byte]$t4)
                    }

                    $at = 0
                    if ($count -le $xoredByte) {
                        $at = 1
                    }
                    
                    if ($at -eq 0) {
                        $a0C = $a0C -shr 1
                        break
                    }
                    $t1C = $t1C -band 0xFFF
                }
            }
            else {
                $a0C = $a0C -shr 1

                $currentByte = $fileStream.ReadByte()
                if ($currentByte -eq -1) {
                    Write-Host "End of stream reached unexpectedly."
                    break
                }
                $xoredByte = $currentByte -bxor 0x72
                $xored.Add([byte]$xoredByte)

                $count = $t1C

                $t1C++
                if ($fileWordCache.Count -gt $count) {
                    $fileWordCache[$count] = [byte]$xoredByte
                }
                else {
                    $fileWordCache.Add([byte]$xoredByte)
                }
                $t1C = $t1C -band 0xFFF
            }
        }
    }

    $fileName = [System.IO.Path]::GetFileName($filePath)
    $directory = [System.IO.Path]::GetDirectoryName($filePath)
    $outputPath = [System.IO.Path]::Combine($directory, $fileName)
    $fileStream.Close()
    [System.IO.File]::WriteAllBytes($outputPath, $xored.ToArray())

    Write-Host "Decompression complete for file type 31: $filePath"
}

function CheckFile {
    param ($filePath)
    
    Write-Host "Checking file: $filePath"
    
    $fileStream = [System.IO.File]::Open($filePath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite)

    $fileMagic = New-Object byte[] 4
    $fileStream.Read($fileMagic, 0, $fileMagic.Length) | Out-Null
    $isValid = ($fileMagic[0] -eq 0x20 -and $fileMagic[1] -eq 0x33 -and $fileMagic[2] -eq 0x3B -and ($fileMagic[3] -eq 0x30 -or $fileMagic[3] -eq 0x31))

    $decompressType = $fileMagic[3]

    if ($isValid) {
        Write-Host "File is valid: $filePath"
        if ($decompressType -eq 0x30) {
            DecompressFileType30 -filePath $filePath -fileStream $fileStream
        }
        else {
            DecompressFileType31 -filePath $filePath -fileStream $fileStream
        }
    }
    else {
        Write-Host "File is not valid: $filePath"
        $fileStream.Close()
    }
}

CheckFile -filePath $filePath