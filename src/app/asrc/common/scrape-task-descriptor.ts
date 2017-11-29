import * as moment from 'moment';



export class ScrapeTaskDescriptor {
    public workingDir: string;
    public guidString: string;
    public diaryUrl: string;
    public scrapeStart: moment.Moment;
    public scrapeEnd: moment.Moment;
    public overwrite: boolean;
    public requestDelay: number;
    public progress: TaskProgress;
    public error: string;
    public status: number;
}



export class TaskProgress {
    percent: number;
    values: StringDictionary;
}

export interface StringDictionary {
    [key: string]: number|string;
}