import * as moment from 'moment';

export class TaskDescriptorBase {
    public workingDir: string;
    public guidString: string;
    public progress: TaskProgress;
    public error: string;
    public status: number;
}

export class ScrapeTaskDescriptor extends TaskDescriptorBase {

    public diaryUrl: string;
    public scrapeStart: moment.Moment;
    public scrapeEnd: moment.Moment;
    public overwrite: boolean;
    public requestDelay: number;
    public downloadEdits: boolean;
    public downloadAccount: boolean;

}

export class ParseTaskDescriptor extends TaskDescriptorBase {

}



export class TaskProgress {
    percent: number;
    values: StringDictionary;
}

export interface StringDictionary {
    [key: string]: number | string;
}