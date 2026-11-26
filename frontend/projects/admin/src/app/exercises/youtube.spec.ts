import { describe, expect, it } from 'vitest';
import { embedUrl, isShort, youTubeId } from './youtube';

// @ADR-f03-thu-vien-bai-tap-005: chỉ nhúng khi rút được mã video 11 ký tự; còn lại chỉ hiện link
describe('youtube', () => {
  it.each([
    ['https://www.youtube.com/watch?v=vcBig73ojpE', 'vcBig73ojpE'],
    ['https://youtube.com/shorts/PfmfAeMpym4', 'PfmfAeMpym4'],
    ['https://www.youtube.com/shorts/PfmfAeMpym4?feature=share', 'PfmfAeMpym4'],
    ['https://youtu.be/abc123XYZ09', 'abc123XYZ09'],
    ['https://www.youtube.com/embed/vcBig73ojpE', 'vcBig73ojpE'],
    ['https://www.youtube.com/watch?feature=share&v=-YcyYvmXZeM', '-YcyYvmXZeM'],
  ])('extracts the video id from %s', (url, id) => {
    expect(youTubeId(url)).toBe(id);
  });

  it('has no id for a playlist, a channel or no link', () => {
    expect(youTubeId('https://www.youtube.com/playlist?list=PL1234567890')).toBeNull();
    expect(youTubeId('https://www.youtube.com/@duobexiuuu')).toBeNull();
    expect(youTubeId(null)).toBeNull();
    expect(youTubeId('')).toBeNull();
  });

  it('tells shorts apart (9:16 frame)', () => {
    expect(isShort('https://youtube.com/shorts/PfmfAeMpym4')).toBe(true);
    expect(isShort('https://www.youtube.com/watch?v=vcBig73ojpE')).toBe(false);
    expect(isShort(null)).toBe(false);
  });

  it('embeds through youtube-nocookie with the prototype parameters', () => {
    expect(embedUrl('PfmfAeMpym4')).toBe('https://www.youtube-nocookie.com/embed/PfmfAeMpym4?rel=0&modestbranding=1&playsinline=1');
  });
});
