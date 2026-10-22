using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// Metadata-only reader for the shipped Unity 2018 serialized-file format (v17).
// Never exports texture pixels, geometry, sound, scripts or localization text.
class AssetIndex {
 static uint BE(BinaryReader r){var b=r.ReadBytes(4);if(b.Length!=4)throw new EndOfStreamException();return ((uint)b[0]<<24)|((uint)b[1]<<16)|((uint)b[2]<<8)|b[3];}
 static string Z(BinaryReader r){var b=new List<byte>();byte c;while((c=r.ReadByte())!=0){if(b.Count>256)throw new InvalidDataException();b.Add(c);}return Encoding.UTF8.GetString(b.ToArray());}
 static void Align(BinaryReader r){r.BaseStream.Position=(r.BaseStream.Position+3)&~3L;}
 static string Name(BinaryReader r,long offset,uint size,int type){long stop=offset+size;r.BaseStream.Position=offset;if(type==1){int n=r.ReadInt32();if(n<0||n>10000)return "";r.BaseStream.Position+=12L*n+4;}else if(type==114){r.BaseStream.Position+=12+4+12;}int length=r.ReadInt32();if(length<1||length>512||r.BaseStream.Position+length>stop)return "";string value=Encoding.UTF8.GetString(r.ReadBytes(length));foreach(char c in value)if(char.IsControl(c))return "";return value;}
 static string Kind(int id){switch(id){case 1:return "GameObject";case 21:return "Material";case 28:return "Texture2D";case 43:return "Mesh";case 48:return "Shader";case 49:return "TextAsset";case 74:return "AnimationClip";case 83:return "AudioClip";case 91:return "AnimatorController";case 114:return "MonoBehaviour";case 115:return "MonoScript";case 128:return "Font";case 213:return "Sprite";case 221:return "AnimatorOverrideController";default:return "ClassID_"+id;}}
 static string Clean(string text){return text.Replace('\t',' ').Replace('\r',' ').Replace('\n',' ');}
 static int Scan(string path,StreamWriter output){using(var stream=File.OpenRead(path))using(var r=new BinaryReader(stream)){
  if(stream.Length<24)return 0;uint metadata=BE(r),length=BE(r),version=BE(r),data=BE(r);if(version!=17||length>stream.Length||data>stream.Length||metadata>stream.Length)return 0;if(r.ReadByte()!=0)throw new NotSupportedException("Big-endian file");r.ReadBytes(3);Z(r);r.ReadInt32();bool tree=r.ReadBoolean();int count=r.ReadInt32();if(count<0||count>10000)throw new InvalidDataException("Type count");var types=new int[count];
  for(int i=0;i<count;i++){int id=r.ReadInt32();types[i]=id;r.ReadBoolean();r.ReadInt16();if(id==114)r.ReadBytes(16);r.ReadBytes(16);if(tree){int nodes=r.ReadInt32(),strings=r.ReadInt32();if(nodes<0||strings<0)throw new InvalidDataException();stream.Position+=nodes*24L+strings;}}
  int objects=r.ReadInt32();if(objects<0||objects>2000000)throw new InvalidDataException("Object count");var file=Path.GetFileName(path);int scene=-1;var match=Regex.Match(file,@"^level(\d+)$");if(match.Success)scene=int.Parse(match.Groups[1].Value);
  for(int i=0;i<objects;i++){Align(r);long id=r.ReadInt64();uint offset=r.ReadUInt32(),size=r.ReadUInt32();int typeIndex=r.ReadInt32();long next=stream.Position;if(typeIndex<0||typeIndex>=types.Length)throw new InvalidDataException("Type index");int type=types[typeIndex];string name="";if((long)data+offset+size<=stream.Length)try{name=Name(r,(long)data+offset,size,type);}catch(EndOfStreamException){}stream.Position=next;output.WriteLine(file+"\t"+id+"\t"+Kind(type)+"\t"+Clean(name)+"\t"+scene);}
  return objects;
 }}
 static int Main(string[] args){try{if(args.Length!=2){Console.Error.WriteLine("AssetIndex.exe <Beholder2_Data> <catalog.tsv>");return 2;}string data=Path.GetFullPath(args[0]);int total=0,files=0,errors=0;Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1])));using(var output=new StreamWriter(args[1]+".tmp",false,new UTF8Encoding(false))){output.WriteLine("file\tpathID\ttype\tname\tsceneIndex");foreach(var path in Directory.GetFiles(data,"*",SearchOption.TopDirectoryOnly)){string file=Path.GetFileName(path);if(!file.EndsWith(".assets")&&!Regex.IsMatch(file,@"^level\d+$")&&file!="globalgamemanagers")continue;try{int n=Scan(path,output);total+=n;files++;Console.WriteLine(file+": "+n);}catch(Exception e){errors++;Console.Error.WriteLine(file+": "+e.Message);}}}if(total==0)throw new InvalidDataException("No supported assets found");File.Copy(args[1]+".tmp",args[1],true);File.Delete(args[1]+".tmp");Console.WriteLine("Indexed "+total+" objects in "+files+" files; errors="+errors);return errors==0?0:1;}catch(Exception e){Console.Error.WriteLine(e.Message);return 2;}}
}
