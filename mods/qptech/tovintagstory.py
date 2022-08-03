import c4d
from c4d import gui
from c4d import documents
#Welcome to the world of Python
#Cinema4d -> VSMC model file exporter by Quentin P

def test(texty):
    gui.MessageDialog(texty)

def getfromv(points):
    minx=9999
    miny=9999
    minz=9999
    maxx=-9999
    maxy=-9999
    maxz=-9999
    for p in points:
        if p.x<minx: minx=p.x
        if p.y<miny: miny=p.y
        if p.z<minz: minz=p.z
        if p.x>maxx: maxx=p.x
        if p.y>maxy: maxy=p.y
        if p.z>maxz: maxz=p.z
    sizex=maxx-minx
    sizey=maxy-miny
    sizez=maxz-minz
    retvector=c4d.Vector(minx,miny,minz)
    return retvector
def gettov(points):
    minx=9999
    miny=9999
    minz=9999
    maxx=-9999
    maxy=-9999
    maxz=-9999
    for p in points:
        if p.x<minx: minx=p.x
        if p.y<miny: miny=p.y
        if p.z<minz: minz=p.z
        if p.x>maxx: maxx=p.x
        if p.y>maxy: maxy=p.y
        if p.z>maxz: maxz=p.z
    sizex=maxx-minx
    sizey=maxy-miny
    sizez=maxz-minz
    retvector=c4d.Vector(maxx,maxy,maxz)
    return retvector
def main():
    radtodeg=57.2958
    #start of json and header
    json="{\n"
    json+='"editor": {"allAngles": false,"entityTextureMode": false},\n'
    json+='"textureWidth": 16,\n"textureHeight": 16,\n"textureSizes": {},\n'

    #material definition
    json+='"textures": {\n'
    actmat = c4d.documents.GetActiveDocument().GetMaterials()
    listctr=0;
    for x in actmat:
      fullname=x.GetName()
      json += ' "'+fullname.split("/",1)[1]+'" :'+'"'+fullname+'"'
      listctr+=1
      if (listctr<len(actmat)):
          json+=',\n'
      else:
          json+='\n'
    json+='},\n'

    #elements - will have to also go thru each sub element
    json+='"elements": [\n'
    objectnumber=0
    actsel = c4d.documents.GetActiveDocument().GetObjects()
    listctr=0;
    for c in actsel:
        if c.GetName()=="scene": continue
        if type(c)!=c4d.PolygonObject: continue
        #setup each object as a cuboid
        json+=' {\n'
        json+='  "name" : "%s%i",\n'%(c.GetName(),objectnumber)
        objectnumber+=1
        #find all the coordinates information (sort of a bounding box)
        startpos=c.GetAbsPos()
        startpos.x=startpos.x/10
        startpos.y=startpos.y/10
        startpos.z=startpos.z/10
        tov = c4d.Vector(0,0,0)
        fromv = c4d.Vector(0,0,0)
        points=c.GetAllPoints()
        fromv = getfromv(points)
        fromv.x=fromv.x/10+startpos.x+8
        fromv.y=fromv.y/10+startpos.y+8
        fromv.z=fromv.z/10+startpos.z+8
        tov = gettov(points)
        tov.x=tov.x/10+startpos.x+8
        tov.y=tov.y/10+startpos.y+8
        tov.z=tov.z/10+startpos.z+8
        #handle rotation information
        rotation=c.GetAbsRot()
        rotation.x*=-radtodeg #H or y rotation in VS
        rotation.y*=-radtodeg #P or x rotation in VS
        rotation.z*=-radtodeg #B or z rotation in VS
        origin=c4d.Vector(8,8,8)
        origin.x+=startpos.x
        origin.y+=startpos.y
        origin.z+=startpos.z
        #find texture information
        tags=c.GetTags()
        texture =''
        for tag in tags:
            if type(tag)==c4d.TextureTag:
                texture=tag.GetMaterial().GetName()
                texture=texture.split("/",1)[1]

        json+=('  "from": [ %f, %f, %f ],\n'%(fromv.x,fromv.y,fromv.z))
        json+='  "to": [ %f, %f, %f ],\n'%(tov.x,tov.y,tov.z)
        json+='  "rotationOrigin": [ %f, %f, %f ],\n'%(origin.x,origin.y,origin.z)
        json+='  "rotationY": %f,\n'%rotation.x
        json+='  "rotationX": %f,\n'%rotation.y
        json+='  "rotationZ": %f,\n'%rotation.z
        json+='  "faces": {\n'
        json+='    "north": { "texture": "#%s", "uv": [ 0.0, 0.0, 16.0, 16.0 ] },\n'%texture
        json+='    "east": { "texture": "#%s", "uv": [ 0.0, 0.0, 16.0, 16.0 ] },\n'%texture
        json+='    "south": { "texture": "#%s", "uv": [ 0.0, 0.0, 16.0, 16.0 ] },\n'%texture
        json+='    "west": { "texture": "#%s", "uv": [ 0.0, 0.0, 16.0, 16.0 ] },\n'%texture
        json+='    "up": { "texture": "#%s", "uv": [ 0.0, 0.0, 16.0, 16.0 ] },\n'%texture
        json+='    "down": { "texture": "#%s", "uv": [ 0.0, 0.0, 16.0, 16.0 ] }\n'%texture
        json+='   }\n'
        json+='  }'
        listctr+=1
        if listctr<len(actsel): json+=","
    json+='\n]\n'
    #end of json
    json+="}"

    #final output
    print (json);

if __name__=='__main__':
    main()