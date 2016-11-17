#include <stdio.h>
#include <stdint.h>
#include <time.h>
#include <direct.h>
#include <sys/utime.h>

#include <functional>
#include <vector>

struct sfp_header
{
	uint32_t magic;
	uint32_t version;
	uint64_t unk1;
	uint64_t firstDirOffset;
	uint64_t nameTableOffset;
	uint64_t dataOffset;
	uint64_t archiveSize;
	uint64_t packageLabelOffset;
	uint64_t unk3;
};

#pragma pack(push, 1)
struct sfp_dir
{
	uint32_t magic;
	uint64_t nameOffset;
	uint32_t unk1;
	uint64_t parentOffset;
	uint32_t isDir; // ?
	uint64_t fileLength;
	uint64_t modifiedTime;
	uint64_t createdTime;
	uint64_t unk2;
	uint64_t unk3;
	uint64_t startOffset;
	uint32_t dataLength;
};
#pragma pack(pop)

void ReadDirectory(const std::string& dirPath, FILE* f, uint64_t dirOffset, const std::function<const char*(uint64_t)>& getName)
{
	_fseeki64(f, dirOffset, SEEK_SET);

	sfp_dir dir;
	fread(&dir, 1, sizeof(dir), f);

	std::string fn = (dirPath + getName(dir.nameOffset));
	printf("%s\n", fn.c_str());

	if (dir.isDir)
	{
		_mkdir(fn.c_str());

		for (uint64_t offset = dir.startOffset; offset < (dir.startOffset + dir.dataLength); offset += sizeof(sfp_dir))
		{
			ReadDirectory(fn + "/", f, offset, getName);
		}
	}
	else
	{
		char buffer[2048];
		FILE* of = fopen(fn.c_str(), "wb");

		if (of)
		{
			_fseeki64(f, dir.startOffset, SEEK_SET);

			for (uint64_t readBytes = 0; readBytes < dir.dataLength; readBytes += sizeof(buffer))
			{
				uint32_t thisRead = sizeof(buffer);

				if ((dir.dataLength - readBytes) < thisRead)
				{
					thisRead = dir.dataLength - readBytes;
				}

				fread(buffer, 1, thisRead, f);
				fwrite(buffer, 1, thisRead, of);
			}

			fclose(of);
		}

		// set times
		__utimbuf64 ut;
		ut.actime = dir.createdTime;
		ut.modtime = dir.createdTime;

		_utime64(fn.c_str(), &ut);
	}
}

int main(int argc, char** argv)
{
	if (argc < 2)
	{
		return 1;
	}

	const char* fname = argv[1];

	sfp_header header;

	FILE* f = fopen(fname, "rb");

	if (!f)
	{
		return 1;
	}

	fread(&header, sizeof(header), 1, f);

	// read name table
	std::vector<char> nameTable(header.dataOffset - header.nameTableOffset);
	_fseeki64(f, header.nameTableOffset, SEEK_SET);

	fread(&nameTable[0], 1, nameTable.size(), f);

	// make root directory
	std::string outRoot = fname;
	outRoot = outRoot.substr(0, outRoot.length() - 4);

	_mkdir(outRoot.c_str());

	// read directories
	ReadDirectory(outRoot, f, header.firstDirOffset, [&] (uint64_t nameOffset) -> const char*
	{
		if (nameOffset == 0)
		{
			return "";
		}

		return &nameTable[nameOffset - header.nameTableOffset];
	});

	fclose(f);

	return 0;
}