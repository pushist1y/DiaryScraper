import { TaskDescriptorBase } from "../common/scrape-task-descriptor";
import { Observable } from "rxjs/Observable";


export interface IRemoteProcessSericeStartArgs {
    descriptor: TaskDescriptorBase;
}

export interface IRemoteProcessService {
    startRemoteProcess(args: IRemoteProcessSericeStartArgs): Observable<TaskDescriptorBase>;
    updateRemoteProcess(guid: string): Observable<TaskDescriptorBase>;
    cancelRemoteProcess(guid: string): Observable<TaskDescriptorBase>;
}