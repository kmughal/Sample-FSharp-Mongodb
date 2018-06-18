
open System
open MongoDB.Bson
open MongoDB.Bson.Serialization.Attributes
open MongoDB.Driver
open MongoDB.Driver.Builders
open MongoDB.Driver.Linq
open MongoDB.FSharp

[<Literal>]
let dbname = "Dbase"

[<Literal>]
let cs = "mongodb://localhost"

[<Literal>]
let defaultCollectionname = "Collection"

type Person = {name:string;age:int;}
type School = {name:string;address:string}

open System
open System.Collections.Generic

 
type MongoSession = {id : string; items : Dictionary<string,Object>}


type MongoStore() =
    let _cs = "mongodb://localhost"
    let _database = "SessionStore"
    let _collectionname = "UserDocument"
    let mutable _collection: IMongoCollection<MongoSession> = null
    let mutable _db:IMongoDatabase = null

    
    let createCollection =
        let client = MongoClient(_cs)
        if client.GetDatabase(_database) = null then  client.GetDatabase(_database).CreateCollection(_collectionname)
        let db = client.GetDatabase(_database)
        db.GetCollection(_collectionname) |> ignore
            

    do createCollection

    let getCollection = 
         MongoClient(_cs).GetDatabase(_database).GetCollection(_collectionname)

    let getCurrentSession id  = 
        try 
            let collection = getCollection
            let r = collection.Find(Builders.Filter.Empty).ToEnumerable()
            let result = r |> Seq.find(fun f -> f.id = id)
            Some result
        with _ ->  None

    member this.Upsert(id: Guid,key:string,input:Object) =
        let _id = id.ToString()
        match getCurrentSession _id with
           | Some r -> 
                let dic = new Dictionary<string,Object>()
                getCollection.DeleteOne<MongoSession>(fun f -> f.id = _id) |> ignore

                if r.items.Count > 0 then for i in r.items do if (i.Key <> key) then dic.Add(i.Key,i.Value)

                dic.Add(key,input)
                let collection = getCollection
                collection.InsertOne({id = _id;items = dic})
           | None -> 
            let dic = new Dictionary<string,Object>()
            dic.Add(key,input)
            let collection = getCollection
            collection.InsertOne({id = _id;items = dic})
           
    
    member this.Get(id:Guid,key:string) =
        let _id = id.ToString()
        match getCurrentSession _id with
           | Some r -> 
                match r.items.ContainsKey key with | true -> r.items |> Seq.find(fun f -> f.Key = key)  | _ -> failwith "key not found"
           | None -> failwith "sesison is out"


[<EntryPoint>]
let main argv = 
    let server = new MongoStore()
    let id = Guid.NewGuid()
    server.Upsert(id,"StudentRecord" , {name = "Khurram";age = 37})
    server.Upsert(id,"StudentRecord" , {name = "Khurram Shahzad Mughal";age = 37})
    server.Upsert(id,"School" , {name = "Govt. shcool";address = "Pakistan"})
    0 