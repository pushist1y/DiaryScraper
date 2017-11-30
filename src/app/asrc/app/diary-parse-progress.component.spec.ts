import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { DiaryParseProgressComponent } from './diary-parse-progress.component';

describe('DiaryParseProgressComponent', () => {
  let component: DiaryParseProgressComponent;
  let fixture: ComponentFixture<DiaryParseProgressComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ DiaryParseProgressComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DiaryParseProgressComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
