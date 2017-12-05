import { Injectable } from "@angular/core";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { ScrapeTaskDescriptor, ParseTaskDescriptor, TaskDescriptorBase } from "../common/scrape-task-descriptor";
import { Observable } from "rxjs/Observable";
import { IRemoteProcessService, IRemoteProcessSericeStartArgs } from "./remote-service-interface";

const httpOptions = {
    headers: new HttpHeaders({ 'Content-Type': 'application/json' })
};

@Injectable()
export class ParseTaskService implements IRemoteProcessService {
    constructor(private http: HttpClient) {

    }

    private apiUrl: string = 'http://localhost:5000/api';

    startRemoteProcess(args: IRemoteProcessSericeStartArgs): Observable<TaskDescriptorBase> {
        let url = this.apiUrl + "/parse";
        return this.http.post<ParseTaskDescriptor>(url, args.descriptor, httpOptions);
    }

    updateRemoteProcess(guid: string): Observable<TaskDescriptorBase> {
        let url = this.apiUrl + "/parse/" + guid;
        return this.http.get<ParseTaskDescriptor>(url);
    }

    cancelRemoteProcess(guid: string): Observable<TaskDescriptorBase> {
        let url = this.apiUrl + "/parse/" + guid;
        return this.http.delete<ParseTaskDescriptor>(url);
    }

}