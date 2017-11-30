import { Injectable } from "@angular/core";
import { HttpClient, HttpHeaders } from "@angular/common/http";
import { ScrapeTaskDescriptor } from "../common/scrape-task-descriptor";
import { Observable } from "rxjs/Observable";

const httpOptions = {
    headers: new HttpHeaders({ 'Content-Type': 'application/json' })
};

@Injectable()
export class ScrapeTaskService {
    constructor(private http: HttpClient) {

    }

    private apiUrl: string = 'http://localhost:5000/api';


    startScraping(task: ScrapeTaskDescriptor, login: string, password: string): Observable<ScrapeTaskDescriptor> {
        let url = this.apiUrl + "/scrape";
        let params = new URLSearchParams();
        params.set("login", login);
        params.set("password", password);
        url += "?" + params.toString();
        return this.http.post<ScrapeTaskDescriptor>(url, task, httpOptions);
    }

    cancelScraping(guid: string): Observable<ScrapeTaskDescriptor>{
        let url = this.apiUrl + "/scrape/" + guid;
        return this.http.delete<ScrapeTaskDescriptor>(url);
    }

    updateScraping(guid: string): Observable<ScrapeTaskDescriptor>{
        let url = this.apiUrl + "/scrape/" + guid;
        return this.http.get<ScrapeTaskDescriptor>(url);
    }
}