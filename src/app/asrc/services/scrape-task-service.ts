import { Injectable } from "@angular/core";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { ScrapeTaskDescriptor, TaskDescriptorBase } from "../common/scrape-task-descriptor";
import { Observable } from "rxjs/Observable";
import { IRemoteProcessSericeStartArgs, IRemoteProcessService } from "./remote-service-interface";
import { RemoteProcessServiceBase } from "./remote-task-service-base";




export interface IRemoteProcessScrapingSericeStartArgs extends IRemoteProcessSericeStartArgs {
    login: string;
    password: string;
}

@Injectable()
export class ScrapeTaskService extends RemoteProcessServiceBase implements IRemoteProcessService {

    apiUrl: string = 'http://localhost:5000/api/scrape/';

    constructor(http: HttpClient) {
        super(http);
    }

    startRemoteProcess(args: IRemoteProcessScrapingSericeStartArgs): Observable<TaskDescriptorBase> {
        let url = this.apiUrl;
        let params = new URLSearchParams();
        params.set("login", args.login);
        params.set("password", args.password);
        url += "?" + params.toString();
        return this.http.post<ScrapeTaskDescriptor>(url, args.descriptor, this.httpOptions);
    }
}