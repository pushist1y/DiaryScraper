import { Injectable } from "@angular/core";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { ScrapeTaskDescriptor, TaskDescriptorBase } from "../common/scrape-task-descriptor";
import { Observable } from "rxjs/Observable";
import { IRemoteProcessSericeStartArgs, IRemoteProcessService } from "./remote-service-interface";

const httpOptions = {
    headers: new HttpHeaders({ 'Content-Type': 'application/json' })
};


export interface IRemoteProcessScrapingSericeStartArgs extends IRemoteProcessSericeStartArgs {
    login: string;
    password: string;
}

@Injectable()
export class ScrapeTaskService implements IRemoteProcessService {

    private apiUrl: string = 'http://localhost:5000/api';

    constructor(private http: HttpClient) {

    }

    startRemoteProcess(args: IRemoteProcessScrapingSericeStartArgs): Observable<TaskDescriptorBase> {
        let url = this.apiUrl + "/scrape";
        let params = new URLSearchParams();
        params.set("login", args.login);
        params.set("password", args.password);
        url += "?" + params.toString();
        return this.http.post<ScrapeTaskDescriptor>(url, args.descriptor, httpOptions);
    }

    updateRemoteProcess(guid: string): Observable<TaskDescriptorBase> {
        let url = this.apiUrl + "/scrape/" + guid;
        return this.http.get<ScrapeTaskDescriptor>(url);
    }
    cancelRemoteProcess(guid: string): Observable<TaskDescriptorBase> {
        let url = this.apiUrl + "/scrape/" + guid;
        return this.http.delete<ScrapeTaskDescriptor>(url);
    }


}