import { Injectable } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { IRemoteProcessService } from "./remote-service-interface";
import { RemoteProcessServiceBase } from "./remote-task-service-base";



@Injectable()
export class ArchiveTaskService extends RemoteProcessServiceBase implements IRemoteProcessService {
    constructor(http: HttpClient) {
        super(http);
    }

    apiUrl: string = 'http://localhost:5000/api/archive/';

}