import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DiaryInputComponent } from './diaryinput.component'
import { DiaryProgressComponent } from './diary-progress.component';
import { DiaryParseInputComponent } from './diary-parse-input.component';
import { DiaryParseProgressComponent } from './diary-parse-progress.component';

const routes: Routes = [
  { path: '', redirectTo: '/input', pathMatch: 'full' },
  { path: 'input', component: DiaryInputComponent },
  { path: 'progress', component: DiaryProgressComponent },
  { path: 'parse', component: DiaryParseInputComponent },
  { path: 'parseprogress', component: DiaryParseProgressComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }