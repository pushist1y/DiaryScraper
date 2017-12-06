import { Injectable } from "@angular/core";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { ScrapeTaskDescriptor, ParseTaskDescriptor, TaskDescriptorBase } from "../common/scrape-task-descriptor";
import { Observable } from "rxjs/Observable";
import { IRemoteProcessService, IRemoteProcessSericeStartArgs } from "./remote-service-interface";


@Injectable()
export abstract class RemoteProcessServiceBase implements IRemoteProcessService {
    constructor(protected http: HttpClient) {

    }

    httpOptions = {
        headers: new HttpHeaders({ 'Content-Type': 'application/json' })
    };

    abstract apiUrl: string;

    startRemoteProcess(args: IRemoteProcessSericeStartArgs): Observable<TaskDescriptorBase> {
        let url = this.apiUrl;
        return this.http.post<ParseTaskDescriptor>(url, args.descriptor, this.httpOptions);
    }

    updateRemoteProcess(guid: string): Observable<TaskDescriptorBase> {
        let url = this.apiUrl + guid;
        return this.http.get<ParseTaskDescriptor>(url);
    }

    cancelRemoteProcess(guid: string): Observable<TaskDescriptorBase> {
        let url = this.apiUrl + guid;
        return this.http.delete<ParseTaskDescriptor>(url);
    }

}