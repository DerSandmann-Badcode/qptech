import c4d
from c4d import gui
from c4d import documents
#Welcome to the world of Python


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
    retvector=c4d.Vector(minx+sizex,miny+sizey,minz+sizez)
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
    retvector=c4d.Vector(maxx+sizex,maxy+sizey,maxz+sizez)
    return retvector
def main():
    #start of json and header
    json="{\n"
    json+='"editor": {"allAngles": false,"entityTextureMode": false},\n'
    json+='"textureWidth": 16,\n"textureHeight": 16,\n"textureSizes": {},\n'

    #material definition
    json+='"textures": {\n'
    actmat = c4d.documents.GetActiveDocument().GetActiveMaterials()
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

    actsel = c4d.documents.GetActiveDocument().GetObjects()
    listctr=0;
    for c in actsel:
       
        json+=' {\n'
        json+='  "name" : "'+c.GetName()+'",\n'
        startpos=c.GetAbsPos()
        startpos.x=startpos.x/10
        startpos.y=startpos.y/10
        startpos.z=startpos.z/10
        tov = c4d.Vector(0,0,0)
        fromv = c4d.Vector(0,0,0)
        if type(c)==c4d.PolygonObject:
            points=c.GetAllPoints()
            fromv = getfromv(points)
            fromv.x=fromv.x/10+startpos.x
            fromv.y=fromv.y/10+startpos.y
            fromv.z=fromv.z/10+startpos.z
            tov = gettov(points)
            tov.x=tov.x/10+startpos.x
            tov.y=tov.y/10+startpos.y
            tov.z=tov.z/10+startpos.z

        json+=('  "from": [ %f, %f, %f ],\n'%(fromv.x,fromv.y,fromv.z))
        json+='  "to": [ %f, %f, %f ],\n'%(tov.x,tov.y,tov.z)

        json+='  "faces": {\n'
        json+='    "north": { "texture": "#cable", "uv": [ 0.0, 0.0, 16.0, 16.0 ] },\n'
        json+='    "east": { "texture": "#cable", "uv": [ 0.0, 0.0, 16.0, 16.0 ] },\n'
        json+='    "south": { "texture": "#cable", "uv": [ 0.0, 0.0, 16.0, 16.0 ] },\n'
        json+='    "west": { "texture": "#cable", "uv": [ 0.0, 0.0, 16.0, 16.0 ] },\n'
        json+='    "up": { "texture": "#cable", "uv": [ 0.0, 0.0, 16.0, 16.0 ] },\n'
        json+='    "down": { "texture": "#cable", "uv": [ 0.0, 0.0, 16.0, 16.0 ] }\n'
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