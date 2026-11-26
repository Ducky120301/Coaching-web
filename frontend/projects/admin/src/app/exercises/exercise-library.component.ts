import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { ICONS, StatusBadgeComponent } from '@coaching/shared';
import { firstValueFrom } from 'rxjs';
import { formatCount, formatRange, formatRest } from './dose-format';
import { Categories, ExerciseDetail, ExerciseListItem } from './exercise.models';
import { ExercisesApi, problemTitle } from './exercises.api';
import { embedUrl, isShort, youTubeId } from './youtube';

const ALL = 'all';
const HIDDEN = 'hidden';
type Tone = 'ok' | 'warn';

/** Câu `video` nguyên văn prototype AdThuVien. */
export const NO_VIDEO = 'Chưa có video. Thêm link để học viên xem.';
/** Câu prototype bỏ "bài thay thế" (non-goal F03). */
export const NO_NOTES = 'Chưa có hướng dẫn kỹ thuật và lỗi hay gặp cho bài này. Bấm Sửa bài để thêm.';

/**
 * Thư viện bài tập (page AdThuVien) — danh sách + chi tiết; bỏ "Đang dùng trong" và "Thêm vào giáo án" (F05).
 * Chip "Đã ẩn" xem bài đã ẩn (@BR-f03-thu-vien-bai-tap-016); tìm/lọc ở server (@BR-f03-thu-vien-bai-tap-018, 019).
 */
@Component({
  selector: 'app-exercise-library',
  imports: [RouterLink, StatusBadgeComponent],
  template: `
    <header class="head">
      <div>
        <h1>Thư viện bài tập</h1>
        <p class="summary">{{ summary() }}</p>
      </div>
      <a class="create" routerLink="/thu-vien/bai-tap/moi">
        <svg aria-hidden="true" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"
             stroke-linecap="round" [innerHTML]="plusIcon"></svg>Thêm bài mới</a>
    </header>

    <div class="search-row">
      <div class="search">
        <label for="q" class="sr-only">Tìm bài tập</label>
        <input id="q" type="search" placeholder="Tìm theo tên bài, gõ xong bấm Enter"
               (keydown.enter)="onSearch($any($event.target).value)" (change)="onSearch($any($event.target).value)" />
      </div>
    </div>

    <div role="tablist" aria-label="Nhóm cơ" class="groups">
      @for (g of groupChips(); track g.id) {
        <button type="button" role="tab" class="chip" [class.on]="group() === g.id" [attr.aria-selected]="group() === g.id"
                (click)="pickGroup(g.id)">{{ g.label }}</button>
      }
    </div>
    <div role="group" aria-label="Dụng cụ" class="equips">
      <span class="equips-label">Dụng cụ</span>
      @for (q of equipChips(); track q.id) {
        <button type="button" class="chip small" [class.on]="equipment() === q.id" [attr.aria-pressed]="equipment() === q.id"
                (click)="pickEquipment(q.id)">{{ q.label }}</button>
      }
    </div>

    @if (message(); as m) {
      <p class="msg" [class.ok]="m.tone === 'ok'" role="status">{{ m.text }}</p>
    }

    <div class="body">
      <div class="table-wrap">
        @if (rows(); as list) {
          @if (list.length > 0) {
            <table>
              <thead>
                <tr>
                  <th scope="col">Bài tập</th>
                  <th scope="col">Nhóm cơ</th>
                  <th scope="col">Dụng cụ</th>
                  <th scope="col">Video mẫu</th>
                </tr>
              </thead>
              <tbody>
                @for (r of list; track r.id) {
                  <tr [class.on]="selectedId() === r.id">
                    <th scope="row">
                      <button type="button" class="pick" [attr.aria-pressed]="selectedId() === r.id" (click)="pick(r.id)">{{ r.name }}</button>
                    </th>
                    <td class="group">{{ r.muscleGroup }}</td>
                    <td class="equip">{{ r.equipment }}</td>
                    <td><span class="video" [class.missing]="!r.hasVideo">{{ r.hasVideo ? 'Xem video' : 'Thiếu video' }}</span></td>
                  </tr>
                }
              </tbody>
            </table>
          } @else {
            <p class="empty">{{ emptyText() }}</p>
          }
        } @else if (failed()) {
          <p class="empty" role="alert">Không tải được thư viện bài tập. Vui lòng tải lại trang.</p>
        }
      </div>

      @if (detail(); as d) {
        <aside aria-labelledby="h-ct">
          <div>
            <p class="meta">{{ d.muscleGroup }}, {{ d.equipment }}</p>
            <h2 id="h-ct">{{ d.name }}</h2>
            @if (d.isHidden) {
              <dbx-status-badge class="hidden-badge" tone="paused" [dense]="true">Đã ẩn</dbx-status-badge>
            }
          </div>

          @if (video(); as v) {
            <div class="video-block">
              @if (v.embed) {
                <div class="frame" [class.short]="v.short">
                  <iframe [src]="v.embed" [title]="'Video mẫu ' + d.name" loading="lazy"
                          allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
                          referrerpolicy="strict-origin-when-cross-origin" allowfullscreen></iframe>
                </div>
              }
              <a class="yt" [href]="v.url" target="_blank" rel="noopener">Mở trên YouTube</a>
            </div>
          } @else {
            <p class="box">{{ noVideo }}</p>
          }

          <dl>
            <div><dt>Sets</dt><dd>{{ dose().sets }}</dd></div>
            <div><dt>Reps</dt><dd>{{ dose().reps }}</dd></div>
            <div><dt>RIR</dt><dd>{{ dose().rir }}</dd></div>
            <div><dt>Nghỉ</dt><dd>{{ dose().rest }}</dd></div>
          </dl>
          @if (dose().empty) {
            <p class="rx">Chưa có liều mặc định.</p>
          }

          @if (d.techniqueNotes || d.commonMistakes) {
            @if (d.techniqueNotes) {
              <section class="notes"><h3>Hướng dẫn kỹ thuật</h3><p>{{ d.techniqueNotes }}</p></section>
            }
            @if (d.commonMistakes) {
              <section class="notes"><h3>Lỗi hay gặp</h3><p>{{ d.commonMistakes }}</p></section>
            }
          } @else {
            <p class="box">{{ noNotes }}</p>
          }

          <div class="actions">
            <a class="secondary edit" [routerLink]="['/thu-vien/bai-tap', d.id, 'sua']">Sửa bài</a>
            <button type="button" class="secondary" [disabled]="busy()" (click)="toggleHidden(d)">{{ d.isHidden ? 'Bỏ ẩn' : 'Ẩn bài' }}</button>
            <button type="button" class="danger" [disabled]="busy()" (click)="remove(d)">Xoá bài</button>
          </div>
        </aside>
      }
    </div>
  `,
  styles: `
    /* AdThuVien main: padding 28px clamp(16px, 3vw, 44px) 48px, gap 22px */
    :host { display: flex; flex-direction: column; gap: 22px; box-sizing: border-box; padding: 28px clamp(16px, 3vw, 44px) 48px; }
    .head { display: flex; flex-wrap: wrap; justify-content: space-between; gap: 16px; align-items: flex-end; }
    h1 { margin: 0; font-family: var(--font-head); font-weight: 800; font-size: 46px; line-height: 1.05; }
    .summary { margin: 6px 0 0; font-size: 14px; color: var(--ink2); }
    .create { height: 50px; padding: 0 26px; border-radius: 999px; border: 0; background: var(--primary); color: var(--onPrimary);
      font-family: var(--font-head); font-weight: 700; font-size: 20px; cursor: pointer; display: inline-flex; align-items: center; gap: 8px;
      text-decoration: none; box-sizing: border-box; }
    .create:hover { color: var(--onPrimary); }
    .search-row { display: flex; flex-wrap: wrap; gap: 10px; align-items: center; }
    .search { flex: 1 1 280px; }
    .sr-only { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); }
    #q { box-sizing: border-box; width: 100%; height: 48px; border-radius: 14px; border: 1px solid var(--field); background: var(--surface);
      color: var(--ink); padding: 0 14px; font: inherit; font-size: 15px; }
    .groups { display: flex; flex-wrap: wrap; gap: 8px; }
    .chip { height: 40px; padding: 0 16px; border-radius: 999px; font: inherit; font-size: 14px; cursor: pointer;
      background: var(--surface); color: var(--ink); border: 1px solid var(--field); }
    .chip.on { background: var(--primary); color: var(--onPrimary); border: 0; font-weight: 600; }
    .chip.small { height: 34px; padding: 0 12px; font-size: 13px; }
    .equips { display: flex; flex-wrap: wrap; gap: 6px; align-items: center; margin-top: -10px; }
    .equips-label { font-size: 13px; color: var(--muted); margin-right: 4px; }
    .msg { margin: 0; border-radius: 12px; padding: 10px 14px; font-size: 14px; font-weight: 600; background: var(--warnBg); color: var(--warn); }
    .msg.ok { background: var(--okBg); color: var(--okInk); }
    .body { display: flex; flex-wrap: wrap; gap: 22px; align-items: flex-start; }
    .table-wrap { flex: 999 1 540px; min-width: 0; overflow-x: auto; border: 1px solid var(--line); border-radius: 12px; background: var(--tableBg); }
    table { width: 100%; min-width: 600px; border-collapse: collapse; font-size: 14px; }
    thead th { padding: 12px 14px; font-weight: 500; font-size: 13px; color: var(--muted); text-align: left; border-bottom: 1px solid var(--line); }
    tbody th, tbody td { padding: 11px 14px; border-bottom: 1px solid var(--rowLine); }
    tbody th { text-align: left; }
    tbody tr:hover { background: var(--rowHover); }
    tbody tr.on { background: var(--soft); }
    td.group { color: var(--ink2); white-space: nowrap; }
    td.equip { color: var(--ink2); }
    .pick { background: none; border: 0; padding: 4px 0; font: inherit; font-weight: 600; color: var(--ink); cursor: pointer; text-align: left; }
    .video { display: inline-block; white-space: nowrap; border-radius: 999px; padding: 3px 10px; font-size: 12px; font-weight: 600;
      background: var(--okBg); color: var(--okInk); }
    .video.missing { background: var(--warnBg); color: var(--warn); }
    .empty { margin: 0; padding: 32px; text-align: center; color: var(--muted); }
    aside { flex: 1 1 320px; max-width: 420px; box-sizing: border-box; background: var(--surface); border: 1px solid var(--line); border-radius: 12px;
      padding: 20px; display: flex; flex-direction: column; gap: 14px; }
    .meta { margin: 0; font-size: 13px; color: var(--primary); font-weight: 600; }
    h2 { margin: 2px 0 0; font-family: var(--font-head); font-weight: 800; font-size: 31px; line-height: 1.1; }
    .hidden-badge { display: inline-block; margin-top: 6px; }
    .video-block { display: flex; flex-direction: column; gap: 6px; }
    /* Nền khung video #000 nguyên văn prototype (letterbox của YouTube), không phải màu thương hiệu */
    .frame { position: relative; width: 100%; border-radius: 12px; overflow: hidden; background: #000; aspect-ratio: 16 / 9; }
    .frame.short { aspect-ratio: 9 / 16; max-width: 260px; align-self: center; margin: 0 auto; }
    iframe { position: absolute; inset: 0; width: 100%; height: 100%; border: 0; }
    .yt { align-self: flex-start; font-size: 13px; font-weight: 600; }
    dl { margin: 0; display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 8px; font-size: 12px; }
    dl > div { background: var(--bg); border-radius: 12px; padding: 8px; }
    dt { color: var(--muted); }
    dd { margin: 2px 0 0; font-weight: 600; font-size: 15px; }
    .rx { margin: -6px 0 0; font-size: 12px; color: var(--muted); }
    .notes { display: flex; flex-direction: column; gap: 6px; }
    .notes h3 { margin: 0; font-size: 15px; font-weight: 600; }
    .notes p { margin: 0; font-size: 14px; line-height: 1.6; color: var(--ink2); white-space: pre-line; }
    .box { margin: 0; font-size: 13px; line-height: 1.55; color: var(--muted); background: var(--bg); border-radius: 10px; padding: 10px 12px; }
    .actions { display: flex; flex-wrap: wrap; gap: 8px; }
    .secondary { box-sizing: border-box; display: inline-flex; align-items: center; height: 44px; padding: 0 22px; border-radius: 999px;
      border: 1px solid var(--field); background: var(--surface); color: var(--ink); font: inherit; font-weight: 600; font-size: 14px;
      cursor: pointer; text-decoration: none; }
    .secondary:hover { color: var(--ink); }
    .danger { height: 44px; padding: 0 12px; border: 0; background: transparent; color: var(--alert); font: inherit; font-weight: 600;
      font-size: 14px; text-decoration: underline; cursor: pointer; }
    .secondary:disabled, .danger:disabled { opacity: .6; cursor: default; }
    @media (max-width: 879.98px) { h1 { font-size: 38px; } }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExerciseLibraryComponent implements OnInit {
  /** Query `?bai=<id>`: chọn sẵn bài (sau khi lưu ở form, bỏ ẩn bài trùng, hoặc Huỷ khi sửa). */
  readonly bai = input<string>();

  private readonly api = inject(ExercisesApi);
  private readonly sanitizer = inject(DomSanitizer);
  /** Chống trả lời đến trễ khi coach bấm chip/tìm liên tiếp. */
  private listSeq = 0;
  private detailSeq = 0;

  protected readonly plusIcon = this.sanitizer.bypassSecurityTrustHtml(ICONS.plus);
  protected readonly noVideo = NO_VIDEO;
  protected readonly noNotes = NO_NOTES;

  protected readonly categories = signal<Categories | null>(null);
  /** Bài không ẩn, không lọc — để đếm theo nhóm cơ và dòng tóm tắt. */
  protected readonly all = signal<ExerciseListItem[]>([]);
  protected readonly rows = signal<ExerciseListItem[] | null>(null);
  protected readonly failed = signal(false);
  protected readonly group = signal(ALL);
  protected readonly equipment = signal(ALL);
  protected readonly query = signal('');
  protected readonly selectedId = signal<string | null>(null);
  protected readonly detail = signal<ExerciseDetail | null>(null);
  protected readonly message = signal<{ tone: Tone; text: string } | null>(null);
  protected readonly busy = signal(false);

  protected readonly filtered = computed(() => this.group() !== ALL || this.equipment() !== ALL || this.query() !== '');

  protected readonly groupChips = computed(() => {
    const cats = this.categories();
    if (!cats) return [];
    const all = this.all();
    return [
      { id: ALL, label: `Tất cả (${all.length})` },
      ...cats.muscleGroups.map((g) => ({ id: g.id, label: `${g.name} (${all.filter((x) => x.muscleGroup === g.name).length})` })),
      { id: HIDDEN, label: 'Đã ẩn' },
    ];
  });

  protected readonly equipChips = computed(() => {
    const cats = this.categories();
    return cats ? [{ id: ALL, label: 'Tất cả' }, ...cats.equipment.map((e) => ({ id: e.id, label: e.name }))] : [];
  });

  protected readonly summary = computed(() => {
    const all = this.all();
    const missing = all.filter((x) => !x.hasVideo).length;
    let text = `${all.length} bài`
      + (all.length === 0 ? '.' : missing ? `, ${missing} bài thiếu video mẫu.` : ', bài nào cũng có video mẫu.');
    const rows = this.rows();
    if (this.filtered() && rows) text += ` Đang hiện ${rows.length} bài.`;
    return text;
  });

  protected readonly emptyText = computed(() => {
    if (this.group() === HIDDEN && this.equipment() === ALL && !this.query()) return 'Chưa có bài nào đang ẩn.';
    return this.filtered() ? 'Không tìm thấy bài phù hợp.' : 'Chưa có bài nào trong thư viện. Bấm Thêm bài mới để thêm bài đầu tiên.';
  });

  /** @ADR-f03-thu-vien-bai-tap-005: chỉ nhúng youtube-nocookie khi rút được mã 11 ký tự; còn lại chỉ link. */
  protected readonly video = computed(() => {
    const url = this.detail()?.videoUrl;
    if (!url) return null;
    const id = youTubeId(url);
    return { url, short: isShort(url), embed: id ? this.sanitizer.bypassSecurityTrustResourceUrl(embedUrl(id)) : null };
  });

  /** @ADR-f03-thu-vien-bai-tap-006: hiển thị "3", "10–12", "1–2", "90s"; trống "—". */
  protected readonly dose = computed(() => {
    const d = this.detail();
    if (!d) return { sets: '—', reps: '—', rir: '—', rest: '—', empty: true };
    return {
      sets: formatCount(d.sets),
      reps: formatRange(d.repsMin, d.repsMax),
      rir: formatRange(d.rirMin, d.rirMax),
      rest: formatRest(d.restSeconds),
      empty: [d.sets, d.repsMin, d.repsMax, d.rirMin, d.rirMax, d.restSeconds].every((v) => v === null),
    };
  });

  async ngOnInit(): Promise<void> {
    try {
      this.categories.set(await firstValueFrom(this.api.categories()));
    } catch {
      this.failed.set(true);
      return;
    }
    await this.reload(false, this.bai() ?? null);
  }

  protected pickGroup(id: string): void {
    this.group.set(id);
    void this.reload();
  }

  protected pickEquipment(id: string): void {
    this.equipment.set(id);
    void this.reload();
  }

  protected onSearch(value: string): void {
    const q = value.trim();
    if (q === this.query()) return; // Enter rồi blur cùng từ khoá: không tìm lại
    this.query.set(q);
    void this.reload();
  }

  protected pick(id: string): void {
    this.message.set(null);
    void this.select(id);
  }

  // @BR-f03-thu-vien-bai-tap-015: ẩn / bỏ ẩn; bài vẫn ở aside để coach bỏ ẩn lại ngay
  protected async toggleHidden(d: ExerciseDetail): Promise<void> {
    this.busy.set(true);
    this.message.set(null);
    try {
      const updated = await firstValueFrom(d.isHidden ? this.api.unhide(d.id) : this.api.hide(d.id));
      this.detail.set(updated);
      this.message.set({
        tone: 'ok',
        text: updated.isHidden ? `Đã ẩn bài "${updated.name}". Xem lại ở mục Đã ẩn.` : `Đã bỏ ẩn bài "${updated.name}".`,
      });
      await this.refresh(true);
    } catch (error) {
      this.message.set({ tone: 'warn', text: problemTitle(error, 'Không lưu được. Vui lòng thử lại.') });
    } finally {
      this.busy.set(false);
    }
  }

  // @BR-f03-thu-vien-bai-tap-017: hỏi xác nhận; 409 exercise.in_use → câu của server (gợi ý ẩn)
  protected async remove(d: ExerciseDetail): Promise<void> {
    if (!window.confirm(`Xoá bài "${d.name}" (${d.equipment}) khỏi thư viện? Không hoàn tác được.`)) return;
    this.busy.set(true);
    this.message.set(null);
    try {
      await firstValueFrom(this.api.remove(d.id));
      this.selectedId.set(null);
      this.detail.set(null);
      this.message.set({ tone: 'ok', text: `Đã xoá bài "${d.name}".` });
      await this.refresh(false);
    } catch (error) {
      this.message.set({ tone: 'warn', text: problemTitle(error, 'Không xoá được bài. Vui lòng thử lại.') });
    } finally {
      this.busy.set(false);
    }
  }

  /** Tải lại danh sách đang lọc; khi đang lọc thì tải thêm danh sách đầy đủ để đếm. */
  private async refresh(keepSelection: boolean): Promise<void> {
    await Promise.all([this.reload(keepSelection), this.filtered() ? this.reloadCounts() : Promise.resolve()]);
  }

  private async reloadCounts(): Promise<void> {
    try {
      this.all.set(await firstValueFrom(this.api.list()));
    } catch {
      /* giữ số đếm cũ */
    }
  }

  private async reload(keepSelection = false, prefer: string | null = null): Promise<void> {
    const seq = ++this.listSeq;
    const group = this.group();
    let rows: ExerciseListItem[];
    try {
      rows = await firstValueFrom(this.api.list({
        q: this.query(),
        muscleGroupId: group === ALL || group === HIDDEN ? null : group,
        equipmentId: this.equipment() === ALL ? null : this.equipment(),
        hidden: group === HIDDEN,
      }));
    } catch {
      if (seq === this.listSeq) {
        this.rows.set(null);
        this.failed.set(true);
      }
      return;
    }
    if (seq !== this.listSeq) return;
    this.failed.set(false);
    this.rows.set(rows);
    if (!this.filtered()) this.all.set(rows);
    if (prefer) return this.select(prefer);
    if (keepSelection) return;
    const current = this.selectedId();
    if (current && rows.some((r) => r.id === current)) return;
    await this.select(rows[0]?.id ?? null);
  }

  private async select(id: string | null): Promise<void> {
    this.selectedId.set(id);
    const seq = ++this.detailSeq;
    if (!id) {
      this.detail.set(null);
      return;
    }
    try {
      const d = await firstValueFrom(this.api.get(id));
      if (seq === this.detailSeq) this.detail.set(d);
    } catch {
      if (seq === this.detailSeq) this.detail.set(null);
    }
  }
}
