import { Injectable } from "@angular/core";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { ScrapeTaskDescriptor, ParseTaskDescriptor } from "../common/scrape-task-descriptor";
import { Observable } from "rxjs/Observable";

const httpOptions = {
    headers: new HttpHeaders({ 'Content-Type': 'application/json' })
};

@Injectable()
export class ParseTaskService {
    constructor(private http: HttpClient) {

    }

    private apiUrl: string = 'http://localhost:5000/api';

    startParsing(task: ParseTaskDescriptor): Observable<ParseTaskDescriptor> {
        let url = this.apiUrl + "/parse";
        return this.http.post<ParseTaskDescriptor>(url, task, httpOptions);
    }

    cancelParsing(guid: string): Observable<ParseTaskDescriptor>{
        let url = this.apiUrl + "/parse/" + guid;
        return this.http.delete<ParseTaskDescriptor>(url);
    }

    updateParsing(guid: string): Observable<ParseTaskDescriptor>{
        let url = this.apiUrl + "/parse/" + guid;
        return this.http.get<ParseTaskDescriptor>(url);
    }
}